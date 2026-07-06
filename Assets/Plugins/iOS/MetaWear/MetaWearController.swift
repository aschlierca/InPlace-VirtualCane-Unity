import Foundation
import MetaWear
import MetaWearCpp

// Unity bridge function
@_silgen_name("UnitySendMessage")
func UnitySendMessage(_ obj: UnsafePointer<CChar>, _ method: UnsafePointer<CChar>, _ msg: UnsafePointer<CChar>)

class MetaWearController {

    static let shared = MetaWearController()

    private init() {
        setupBluetoothStateMonitoring()
    }

    var device: MetaWear?
    var board: OpaquePointer?

    var discoveredDevices: [MetaWear] = []
    var isScanning = false

    var userHeightCm: Float = 170

    // MARK: - Configuration

    // IMU_PLUS (acc + gyro) = drift-free pitch/roll, slowly drifting yaw (use in-app recalibrate).
    // NDOF adds the magnetometer for absolute yaw but is jumpy indoors near steel.
    let fusionMode = MBL_MW_SENSOR_FUSION_MODE_IMU_PLUS

    // Stream fusion-corrected accel/gyro for the DataLogger. While fusion runs,
    // raw BMI270 signals must NOT be configured separately — fusion owns them.
    let streamCorrectedRawForLogging = true

    var lastAccel = SIMD3<Float>(0, 0, 0)
    var lastGyro  = SIMD3<Float>(0, 0, 0)

    // MARK: - Bluetooth State Monitoring

    private func setupBluetoothStateMonitoring() {
        MetaWearScanner.sharedRestore.didUpdateState = { [weak self] central in
            print("MetaWear Bluetooth State: \(central.state.rawValue)")

            switch central.state {
            case .poweredOn:
                print("Bluetooth is powered on and ready")
            case .poweredOff:
                print("Bluetooth is powered off")
                self?.sendConnectionStatus(false)
            case .unsupported:
                print("Bluetooth is not supported on this device")
            case .unauthorized:
                print("Bluetooth is not authorized")
            case .resetting:
                print("Bluetooth is resetting")
            case .unknown:
                print("Bluetooth state is unknown")
            @unknown default:
                print("Unknown bluetooth state")
            }
        }
    }

    // MARK: - Scanning

    func startScanning() {
        guard !isScanning else {
            print("Already scanning, ignoring request")
            return
        }

        print("Starting MetaWear scan...")
        discoveredDevices.removeAll()
        isScanning = true

        MetaWearScanner.sharedRestore.startScan(allowDuplicates: true) { [weak self] device in
            guard let self = self else { return }

            print("Discovered device: \(device.name) RSSI: \(device.rssi)")

            guard device.rssi > -80 else {
                print("Skipping: RSSI too weak (\(device.rssi))")
                return
            }

            if !self.discoveredDevices.contains(where: { $0.peripheral.identifier == device.peripheral.identifier }) {
                print("New device discovered: \(device.name)")
                self.discoveredDevices.append(device)

                if self.device == nil {
                    print("Auto-connecting to \(device.name)...")
                    MetaWearScanner.sharedRestore.stopScan()
                    self.isScanning = false
                    self.connect(to: device)
                }
            }
        }
    }

    func stopScanning() {
        guard isScanning else { return }

        print("Stopping MetaWear scan")
        MetaWearScanner.sharedRestore.stopScan()
        isScanning = false
    }

    // MARK: - Connect

    func connect(to device: MetaWear) {
        print("Connecting to MetaWear: \(device.name)")
        self.device = device

        device.connectAndSetup().continueWith { [weak self] task in
            guard let self = self else { return }

            if let error = task.error {
                print("Connection error: \(error.localizedDescription)")
                self.sendConnectionStatus(false)

                DispatchQueue.main.asyncAfter(deadline: .now() + 2.0) { [weak self] in
                    print("Retrying connection...")
                    self?.connect(to: device)
                }
                return
            }

            print("Connected to MetaWear successfully")
            self.board = device.board

            self.startStreaming()
            self.sendConnectionStatus(true)

            task.result?.continueWith { [weak self] _ in
                self?.handleDisconnection()
            }
        }
    }

    // MARK: - Disconnect Handling

    func handleDisconnection() {
        print("MetaWear disconnected")
        sendConnectionStatus(false)

        if let device = device {
            print("Attempting to reconnect...")
            DispatchQueue.main.asyncAfter(deadline: .now() + 1.0) { [weak self] in
                self?.connect(to: device)
            }
        } else {
            print("No device available, restarting scan...")
            DispatchQueue.main.asyncAfter(deadline: .now() + 1.0) { [weak self] in
                self?.startScanning()
            }
        }
    }

    // MARK: - Streaming (Sensor Fusion)

    func startStreaming() {
        guard let board = board else {
            print("Cannot start streaming: board is nil")
            return
        }

        print("Starting sensor fusion streaming...")

        mbl_mw_sensor_fusion_set_mode(board, fusionMode)
        mbl_mw_sensor_fusion_set_acc_range(board, MBL_MW_SENSOR_FUSION_ACC_RANGE_8G)
        mbl_mw_sensor_fusion_set_gyro_range(board, MBL_MW_SENSOR_FUSION_GYRO_RANGE_2000DPS)
        mbl_mw_sensor_fusion_write_config(board)

        guard let quatSignal = mbl_mw_sensor_fusion_get_data_signal(board, MBL_MW_SENSOR_FUSION_DATA_QUATERNION) else {
            print("ERROR: quaternion signal unavailable — update board firmware via the MetaBase app")
            return
        }

        mbl_mw_datasignal_subscribe(quatSignal, bridge(obj: self)) { (context, data) in
            let controller: MetaWearController = bridge(ptr: context!)
            let q: MblMwQuaternion = data!.pointee.valueAs()
            controller.sendQuaternion(w: q.w, x: q.x, y: q.y, z: q.z)
        }
        mbl_mw_sensor_fusion_enable_data(board, MBL_MW_SENSOR_FUSION_DATA_QUATERNION)

        if streamCorrectedRawForLogging {
            if let accSignal = mbl_mw_sensor_fusion_get_data_signal(board, MBL_MW_SENSOR_FUSION_DATA_CORRECTED_ACC) {
                mbl_mw_datasignal_subscribe(accSignal, bridge(obj: self)) { (context, data) in
                    let controller: MetaWearController = bridge(ptr: context!)
                    let v: MblMwCorrectedCartesianFloat = data!.pointee.valueAs()
                    controller.lastAccel = SIMD3<Float>(v.x, v.y, v.z)
                }
                mbl_mw_sensor_fusion_enable_data(board, MBL_MW_SENSOR_FUSION_DATA_CORRECTED_ACC)
            }
            if let gyroSignal = mbl_mw_sensor_fusion_get_data_signal(board, MBL_MW_SENSOR_FUSION_DATA_CORRECTED_GYRO) {
                mbl_mw_datasignal_subscribe(gyroSignal, bridge(obj: self)) { (context, data) in
                    let controller: MetaWearController = bridge(ptr: context!)
                    let v: MblMwCorrectedCartesianFloat = data!.pointee.valueAs()
                    controller.lastGyro = SIMD3<Float>(v.x, v.y, v.z)
                    controller.sendRawPacket()
                }
                mbl_mw_sensor_fusion_enable_data(board, MBL_MW_SENSOR_FUSION_DATA_CORRECTED_GYRO)
            }
        }

        mbl_mw_sensor_fusion_start(board)
        print("Sensor fusion streaming started")
    }

    func stopStreaming() {
        guard let board = board else {
            print("Cannot stop streaming: board is nil")
            return
        }

        print("Stopping sensor fusion streaming...")
        mbl_mw_sensor_fusion_stop(board)
        mbl_mw_sensor_fusion_clear_enabled_mask(board)

        if let quatSignal = mbl_mw_sensor_fusion_get_data_signal(board, MBL_MW_SENSOR_FUSION_DATA_QUATERNION) {
            mbl_mw_datasignal_unsubscribe(quatSignal)
        }
        if let accSignal = mbl_mw_sensor_fusion_get_data_signal(board, MBL_MW_SENSOR_FUSION_DATA_CORRECTED_ACC) {
            mbl_mw_datasignal_unsubscribe(accSignal)
        }
        if let gyroSignal = mbl_mw_sensor_fusion_get_data_signal(board, MBL_MW_SENSOR_FUSION_DATA_CORRECTED_GYRO) {
            mbl_mw_datasignal_unsubscribe(gyroSignal)
        }
        print("Streaming stopped")
    }

    // MARK: - Send to Unity

    func sendQuaternion(w: Float, x: Float, y: Float, z: Float) {
        let epoch = Int64(Date().timeIntervalSince1970 * 1000)
        let json = "{\"epoch\":\(epoch),\"qw\":\(w),\"qx\":\(x),\"qy\":\(y),\"qz\":\(z)}"

        json.withCString { jsonPtr in
            "SensorDataReceiver".withCString { objPtr in
                "OnQuaternionData".withCString { methodPtr in
                    UnitySendMessage(objPtr, methodPtr, jsonPtr)
                }
            }
        }
    }

    func sendRawPacket() {
        let epoch = Int64(Date().timeIntervalSince1970 * 1000)
        let json = "{\"epoch\":\(epoch),\"ax\":\(lastAccel.x),\"ay\":\(lastAccel.y),\"az\":\(lastAccel.z),\"gx\":\(lastGyro.x),\"gy\":\(lastGyro.y),\"gz\":\(lastGyro.z)}"

        json.withCString { jsonPtr in
            "SensorDataReceiver".withCString { objPtr in
                "OnSensorData".withCString { methodPtr in
                    UnitySendMessage(objPtr, methodPtr, jsonPtr)
                }
            }
        }
    }

    // MARK: - Connection Status UI

    func sendConnectionStatus(_ connected: Bool) {
        let msg = connected ? "connected" : "disconnected"
        print("Sending connection status to Unity: \(msg)")

        msg.withCString { msgPtr in
            "SensorDataReceiver".withCString { objPtr in
                "OnConnectionStatus".withCString { methodPtr in
                    UnitySendMessage(objPtr, methodPtr, msgPtr)
                }
            }
        }
    }
}
