//
//  MetaWearBridge.swift
//  Unity-iPhone
//
//  Created by Seoyeon Cho on 2/18/26.
//

import Foundation
import MetaWear
import MetaWearCpp

@_silgen_name("UnitySendMessage")
func UnitySendMessage(_ obj: UnsafePointer<CChar>, _ method: UnsafePointer<CChar>, _ msg: UnsafePointer<CChar>)

@objc public class MetaWearBridge: NSObject {
    
    @objc public static let shared = MetaWearBridge()

    private var discovered: [String: MetaWear] = [:]
    private var selectedUUID: String?
    private var selectedDevice: MetaWear? {
        didSet {
            // Optionally persist selection if desired
        }
    }
    // Unity receiver configuration
    private var accelTarget: String = "MetaWearReceiver"
    private var accelMethod: String = "OnAccelData"
    private var gyroTarget: String = "MetaWearReceiver"
    private var gyroMethod: String = "OnGyroData"

    // Streaming state
    private var accelSignal: OpaquePointer?
    private var gyroSignal: OpaquePointer?
    private var isStreaming = false

    @objc public func startScan() {
        MetaWearScanner.shared.startScan(allowDuplicates: true) { [weak self] device in
            guard let self = self else { return }
            let uuid = device.peripheral.identifier.uuidString
            self.discovered[uuid] = device
            // If a device was previously selected by UUID, update the selectedDevice reference when seen
            if let selected = self.selectedUUID, selected == uuid {
                self.selectedDevice = device
            }
        }
    }

    @objc public func stopScan() {
        MetaWearScanner.shared.stopScan()
    }

    @objc public func select(_ uuid: String) {
        selectedUUID = uuid
        // Try to resolve immediately from current discoveries or scanner map
        if let device = discovered[uuid] {
            selectedDevice = device
            return
        }
        if let device = MetaWearScanner.shared.deviceMap.values.first(where: { $0.peripheral.identifier.uuidString == uuid }) {
            selectedDevice = device
            return
        }
        // Not found yet; will be resolved when seen during scanning
        selectedDevice = nil
    }

    @objc public func connectSelected() {
        // Ensure we have a device reference. If not, try to find it now.
        if selectedDevice == nil, let uuid = selectedUUID {
            if let device = discovered[uuid] ?? MetaWearScanner.shared.deviceMap.values.first(where: { $0.peripheral.identifier.uuidString == uuid }) {
                selectedDevice = device
            }
        }
        guard let device = selectedDevice else {
            print("MetaWearBridge: No selected device to connect.")
            return
        }
        device.connectAndSetup().continueWith { t in
            if let error = t.error {
                print("Connection error:", error)
            } else {
                print("Connected to \(device.peripheral.identifier.uuidString)")
                // Example: flash LED to indicate connection
                var pattern = MblMwLedPattern()
                mbl_mw_led_load_preset_pattern(&pattern, MBL_MW_LED_PRESET_PULSE)
                mbl_mw_led_stop_and_clear(device.board)
                mbl_mw_led_write_pattern(device.board, &pattern, MBL_MW_LED_COLOR_GREEN)
                mbl_mw_led_play(device.board)
            }
            return nil
        }
    }

    @objc public func setUnityAccelTarget(_ objectName: String, method: String) {
        accelTarget = objectName
        accelMethod = method
    }

    @objc public func setUnityGyroTarget(_ objectName: String, method: String) {
        gyroTarget = objectName
        gyroMethod = method
    }

    @objc public func startStreaming() {
        guard !isStreaming else { return }
        // Ensure we have a selected and connected device reference
        if selectedDevice == nil, let uuid = selectedUUID {
            if let device = discovered[uuid] ?? MetaWearScanner.shared.deviceMap.values.first(where: { $0.peripheral.identifier.uuidString == uuid }) {
                selectedDevice = device
            }
        }
        guard let device = selectedDevice else {
            print("MetaWearBridge: No selected device to stream from.")
            return
        }
        let board = device.board!

        // ACCELEROMETER CONFIG
        // Set a reasonable range and ODR, then write config
        _ = mbl_mw_acc_set_range(board, 4.0)
        _ = mbl_mw_acc_set_odr(board, 100.0)
        mbl_mw_acc_write_acceleration_config(board)

        if let sig = mbl_mw_acc_get_acceleration_data_signal(board) {
            accelSignal = sig
            mbl_mw_datasignal_subscribe(sig, bridge(obj: self)) { (context, dataPtr) in
                guard let dataPtr = dataPtr else { return }
                let data = dataPtr.pointee
                let v: MblMwCartesianFloat = data.valueAs()
                let epoch = data.epoch
                // Build JSON string {"type":"accel","epoch":...,"x":...,"y":...,"z":...}
                let json = String(format: "{\"type\":\"accel\",\"epoch\":%lld,\"x\":%.6f,\"y\":%.6f,\"z\":%.6f}", epoch, v.x, v.y, v.z)
                self.sendUnityMessage(target: self.accelTarget, method: self.accelMethod, message: json)
            }
            mbl_mw_acc_enable_acceleration_sampling(board)
            mbl_mw_acc_start(board)
        } else {
            print("MetaWearBridge: Accelerometer signal not available.")
        }

        // GYROSCOPE CONFIG
        let gyroImpl = mbl_mw_metawearboard_lookup_module(board, MBL_MW_MODULE_GYRO)
        if gyroImpl != MBL_MW_MODULE_TYPE_NA {
            switch UInt32(gyroImpl) {
            case UInt32(MBL_MW_MODULE_GYRO_TYPE_BMI160):
                mbl_mw_gyro_bmi160_set_range(board, MBL_MW_GYRO_BOSCH_RANGE_2000dps)
                mbl_mw_gyro_bmi160_set_odr(board, MBL_MW_GYRO_BOSCH_ODR_100Hz)
                mbl_mw_gyro_bmi160_write_config(board)
                if let sig = mbl_mw_gyro_bmi160_get_rotation_data_signal(board) {
                    gyroSignal = sig
                    mbl_mw_datasignal_subscribe(sig, bridge(obj: self)) { (context, dataPtr) in
                        guard let dataPtr = dataPtr else { return }
                        let data = dataPtr.pointee
                        let v: MblMwCartesianFloat = data.valueAs()
                        let epoch = data.epoch
                        let json = String(format: "{\"type\":\"gyro\",\"epoch\":%lld,\"x\":%.6f,\"y\":%.6f,\"z\":%.6f}", epoch, v.x, v.y, v.z)
                        self.sendUnityMessage(target: self.gyroTarget, method: self.gyroMethod, message: json)
                    }
                    mbl_mw_gyro_bmi160_enable_rotation_sampling(board)
                    mbl_mw_gyro_bmi160_start(board)
                } else {
                    print("MetaWearBridge: Gyro BMI160 signal not available.")
                }
            case UInt32(MBL_MW_MODULE_GYRO_TYPE_BMI270):
                mbl_mw_gyro_bmi270_set_range(board, MBL_MW_GYRO_BOSCH_RANGE_2000dps)
                mbl_mw_gyro_bmi270_set_odr(board, MBL_MW_GYRO_BOSCH_ODR_100Hz)
                mbl_mw_gyro_bmi270_write_config(board)
                if let sig = mbl_mw_gyro_bmi270_get_rotation_data_signal(board) {
                    gyroSignal = sig
                    mbl_mw_datasignal_subscribe(sig, bridge(obj: self)) { (context, dataPtr) in
                        guard let dataPtr = dataPtr else { return }
                        let data = dataPtr.pointee
                        let v: MblMwCartesianFloat = data.valueAs()
                        let epoch = data.epoch
                        let json = String(format: "{\"type\":\"gyro\",\"epoch\":%lld,\"x\":%.6f,\"y\":%.6f,\"z\":%.6f}", epoch, v.x, v.y, v.z)
                        self.sendUnityMessage(target: self.gyroTarget, method: self.gyroMethod, message: json)
                    }
                    mbl_mw_gyro_bmi270_enable_rotation_sampling(board)
                    mbl_mw_gyro_bmi270_start(board)
                } else {
                    print("MetaWearBridge: Gyro BMI270 signal not available.")
                }
            default:
                print("MetaWearBridge: Unsupported gyro implementation: \(gyroImpl)")
            }
        } else {
            print("MetaWearBridge: Gyroscope module not available.")
        }

        isStreaming = true
    }

    @objc public func stopStreaming() {
        guard isStreaming, let device = selectedDevice else { return }
        let board = device.board!

        if let sig = accelSignal {
            mbl_mw_acc_stop(board)
            mbl_mw_acc_disable_acceleration_sampling(board)
            mbl_mw_datasignal_unsubscribe(sig)
            accelSignal = nil
        }

        let gyroImpl = mbl_mw_metawearboard_lookup_module(board, MBL_MW_MODULE_GYRO)
        if gyroImpl != MBL_MW_MODULE_TYPE_NA {
            switch UInt32(gyroImpl) {
            case UInt32(MBL_MW_MODULE_GYRO_TYPE_BMI160):
                if let sig = gyroSignal {
                    mbl_mw_gyro_bmi160_stop(board)
                    mbl_mw_gyro_bmi160_disable_rotation_sampling(board)
                    mbl_mw_datasignal_unsubscribe(sig)
                }
            case UInt32(MBL_MW_MODULE_GYRO_TYPE_BMI270):
                if let sig = gyroSignal {
                    mbl_mw_gyro_bmi270_stop(board)
                    mbl_mw_gyro_bmi270_disable_rotation_sampling(board)
                    mbl_mw_datasignal_unsubscribe(sig)
                }
            default:
                break
            }
            gyroSignal = nil
        }

        isStreaming = false
    }

    private func sendUnityMessage(target: String, method: String, message: String) {
        target.withCString { objC in
            method.withCString { mC in
                message.withCString { msgC in
                    UnitySendMessage(objC, mC, msgC)
                }
            }
        }
    }
}

@_cdecl("mw_start_scan")
public func mw_start_scan() {
    MetaWearBridge.shared.startScan()
}
@_cdecl("mw_stop_scan")
public func mw_stop_scan() {
    MetaWearBridge.shared.stopScan()
}

@_cdecl("mw_select_device")
public func mw_select_device(_ uuid: UnsafePointer<CChar>?) {
    guard let uuid = uuid else { return }
    let id = String(cString: uuid)
    MetaWearBridge.shared.select(id)
}

@_cdecl("mw_connect_selected")
public func mw_connect_selected() {
    MetaWearBridge.shared.connectSelected()
}

