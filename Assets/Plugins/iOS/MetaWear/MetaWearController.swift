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
    
    // Track discovered devices
    var discoveredDevices: [MetaWear] = []
    var isScanning = false

    var lastAccel = SIMD3<Float>(0,0,0)
    var lastGyro = SIMD3<Float>(0,0,0)

    var userHeightCm: Float = 170

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
            
            // Log all discovered devices for debugging
            print("Discovered device: \(device.name) RSSI: \(device.rssi)")
            
            // Filter by RSSI (made less restrictive)
            guard device.rssi > -80 else {
                print("   ↳ Skipping: RSSI too weak (\(device.rssi))")
                return
            }
            
            // Check if we already discovered this device
            if !self.discoveredDevices.contains(where: { $0.peripheral.identifier == device.peripheral.identifier }) {
                print("New device discovered: \(device.name)")
                self.discoveredDevices.append(device)
                
                // Auto-connect to the first suitable device
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
                
                // Retry connection after delay
                DispatchQueue.main.asyncAfter(deadline: .now() + 2.0) { [weak self] in
                    print("Retrying connection...")
                    self?.connect(to: device)
                }
                return
            }

            print("Connected to MetaWear successfully")
            self.board = device.board
            
            // Log device information
            if let mac = device.mac {
                print("   MAC: \(mac)")
            }
            if let info = device.info {
                print("   Model: \(info.modelDescription)")
                print("   Hardware: \(info.hardwareRevision)")
                print("   Firmware: \(info.firmwareRevision)")
            }

            self.startStreaming()
            self.sendConnectionStatus(true)
            
            // Handle disconnection
            task.result?.continueWith { [weak self] _ in
                self?.handleDisconnection()
            }
        }
    }

    // MARK: - Disconnect Handling

    func handleDisconnection() {
        print("MetaWear disconnected")
        sendConnectionStatus(false)
        
        // Attempt to reconnect
        if let device = device {
            print("Attempting to reconnect...")
            DispatchQueue.main.asyncAfter(deadline: .now() + 1.0) { [weak self] in
                self?.connect(to: device)
            }
        } else {
            // If no device is set, restart scanning
            print("No device available, restarting scan...")
            DispatchQueue.main.asyncAfter(deadline: .now() + 1.0) { [weak self] in
                self?.startScanning()
            }
        }
    }

    // MARK: - Streaming

    func startStreaming() {
        guard let board = board else {
            print("Cannot start streaming: board is nil")
            return
        }

        print("📊 Starting sensor streaming...")
        startAccelerometer(board: board)
        startGyroscope(board: board)
        print("Streaming started")
    }

    func stopStreaming() {
        guard let board = board else {
            print("Cannot stop streaming: board is nil")
            return
        }

        print("Stopping sensor streaming...")
        stopAccelerometer(board: board)
        stopGyroscope(board: board)
        print("Streaming stopped")
    }

    // MARK: - Accelerometer

    func startAccelerometer(board: OpaquePointer) {
        print("Configuring accelerometer...")
        
        mbl_mw_acc_bosch_set_range(board, MBL_MW_ACC_BOSCH_RANGE_4G)
        mbl_mw_acc_set_odr(board, 100.0)
        mbl_mw_acc_bosch_write_acceleration_config(board)

        let signal = mbl_mw_acc_bosch_get_acceleration_data_signal(board)!

        mbl_mw_datasignal_subscribe(signal, bridge(obj: self)) { (context, data) in
            let controller: MetaWearController = bridge(ptr: context!)
            let accel: MblMwCartesianFloat = data!.pointee.valueAs()
            controller.onAccelData(x: accel.x, y: accel.y, z: accel.z)
        }

        mbl_mw_acc_enable_acceleration_sampling(board)
        mbl_mw_acc_start(board)
        
        print("Accelerometer streaming started")
    }

    func stopAccelerometer(board: OpaquePointer) {
        mbl_mw_acc_stop(board)
        mbl_mw_acc_disable_acceleration_sampling(board)

        let signal = mbl_mw_acc_bosch_get_acceleration_data_signal(board)!
        mbl_mw_datasignal_unsubscribe(signal)
    }

    // MARK: - Gyroscope

    func startGyroscope(board: OpaquePointer) {
        print("Configuring gyroscope...")
        
        mbl_mw_gyro_bmi270_set_range(board, MBL_MW_GYRO_BOSCH_RANGE_2000dps)
        mbl_mw_gyro_bmi270_set_odr(board, MBL_MW_GYRO_BOSCH_ODR_100Hz)
        mbl_mw_gyro_bmi270_write_config(board)

        let signal = mbl_mw_gyro_bmi270_get_rotation_data_signal(board)!

        mbl_mw_datasignal_subscribe(signal, bridge(obj: self)) { (context, data) in
            let controller: MetaWearController = bridge(ptr: context!)
            let gyro: MblMwCartesianFloat = data!.pointee.valueAs()
            controller.onGyroData(x: gyro.x, y: gyro.y, z: gyro.z)
        }

        mbl_mw_gyro_bmi270_enable_rotation_sampling(board)
        mbl_mw_gyro_bmi270_start(board)
        
        print("Gyroscope streaming started")
    }

    func stopGyroscope(board: OpaquePointer) {
        mbl_mw_gyro_bmi270_stop(board)
        mbl_mw_gyro_bmi270_disable_rotation_sampling(board)

        let signal = mbl_mw_gyro_bmi270_get_rotation_data_signal(board)!
        mbl_mw_datasignal_unsubscribe(signal)
    }

    // MARK: - Sensor Callbacks

    func onAccelData(x: Float, y: Float, z: Float) {
        lastAccel = SIMD3<Float>(x, y, z)
        sendPacket()
    }

    func onGyroData(x: Float, y: Float, z: Float) {
        lastGyro = SIMD3<Float>(x, y, z)
        sendPacket()
    }

    // MARK: - Send to Unity

    func sendPacket() {
        let epoch = Int64(Date().timeIntervalSince1970 * 1000)

        let json = """
        {"epoch":\(epoch),"ax":\(lastAccel.x),"ay":\(lastAccel.y),"az":\(lastAccel.z),"gx":\(lastGyro.x),"gy":\(lastGyro.y),"gz":\(lastGyro.z)}
        """

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
