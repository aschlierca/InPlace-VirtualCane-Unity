import Foundation
import MetaWear
import MetaWearCpp

// Unity bridge function
@_silgen_name("UnitySendMessage")
func UnitySendMessage(_ obj: UnsafePointer<CChar>, _ method: UnsafePointer<CChar>, _ msg: UnsafePointer<CChar>)

class MetaWearController {

    static let shared = MetaWearController()

    private init() {}

    var device: MetaWear?
    var board: OpaquePointer?

    var lastAccel = SIMD3<Float>(0,0,0)
    var lastGyro = SIMD3<Float>(0,0,0)

    var userHeightCm: Float = 170

    // MARK: - Scanning

    func startScanning() {

        print("MetaWear scanning...")

        MetaWearScanner.sharedRestore.startScan(allowDuplicates: true) { [weak self] device in

            guard device.rssi > -70 else { return }

            MetaWearScanner.sharedRestore.stopScan()

            self?.connect(to: device)
        }
    }

    // MARK: - Connect

    func connect(to device: MetaWear) {

        print("Connecting to MetaWear")

        self.device = device

        device.connectAndSetup().continueWith { [weak self] task in

            if let error = task.error {
                print("Connection error \(error)")
                return
            }

            guard let self = self else { return }

            self.board = device.board

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
        sendConnectionStatus(false)
        print("MetaWear disconnected — reconnecting")
        
        if let device = device {
            connect(to: device)
        }
    }

    // MARK: - Streaming

    func startStreaming() {

        guard let board = board else { return }

        startAccelerometer(board: board)
        startGyroscope(board: board)
    }

    func stopStreaming() {

        guard let board = board else { return }

        stopAccelerometer(board: board)
        stopGyroscope(board: board)
    }

    // MARK: - Accelerometer

    func startAccelerometer(board: OpaquePointer) {

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
    }

    func stopAccelerometer(board: OpaquePointer) {

        mbl_mw_acc_stop(board)
        mbl_mw_acc_disable_acceleration_sampling(board)

        let signal = mbl_mw_acc_bosch_get_acceleration_data_signal(board)!
        mbl_mw_datasignal_unsubscribe(signal)
    }

    // MARK: - Gyroscope

    func startGyroscope(board: OpaquePointer) {

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
    }

    func stopGyroscope(board: OpaquePointer) {

        mbl_mw_gyro_bmi270_stop(board)
        mbl_mw_gyro_bmi270_disable_rotation_sampling(board)

        let signal = mbl_mw_gyro_bmi270_get_rotation_data_signal(board)!
        mbl_mw_datasignal_unsubscribe(signal)
    }

    // MARK: - Sensor Callbacks

    func onAccelData(x: Float, y: Float, z: Float) {

        lastAccel = SIMD3<Float>(x,y,z)

        sendPacket()
    }

    func onGyroData(x: Float, y: Float, z: Float) {

        lastGyro = SIMD3<Float>(x,y,z)

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

        msg.withCString { msgPtr in
            "SensorDataReceiver".withCString { objPtr in
                "OnConnectionStatus".withCString { methodPtr in
                    UnitySendMessage(objPtr, methodPtr, msgPtr)
                }
            }
        }
    }
}
