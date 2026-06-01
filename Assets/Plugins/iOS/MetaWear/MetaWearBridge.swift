import Foundation

@_cdecl("MetaWear_StartScan")
public func MetaWear_StartScan() {
    print("Unity called: MetaWear_StartScan")
    MetaWearController.shared.startScanning()
}

@_cdecl("MetaWear_StopScan")
public func MetaWear_StopScan() {
    print("Unity called: MetaWear_StopScan")
    MetaWearController.shared.stopScanning()
}

@_cdecl("MetaWear_StopStreaming")
public func MetaWear_StopStreaming() {
    print("Unity called: MetaWear_StopStreaming")
    MetaWearController.shared.stopStreaming()
}

@_cdecl("MetaWear_StartStreaming")
public func MetaWear_StartStreaming() {
    print("Unity called: MetaWear_StartStreaming")
    MetaWearController.shared.startStreaming()
}

@_cdecl("MetaWear_Disconnect")
public func MetaWear_Disconnect() {
    print("Unity called: MetaWear_Disconnect")
    if let device = MetaWearController.shared.device {
        device.cancelConnection()
    }
}

@_cdecl("MetaWear_SetUserHeight")
public func MetaWear_SetUserHeight(_ heightCm: Float) {
    print("Unity called: MetaWear_SetUserHeight(\(heightCm))")
    MetaWearController.shared.userHeightCm = heightCm
}
