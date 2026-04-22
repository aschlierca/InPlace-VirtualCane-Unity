import Foundation

@_cdecl("MetaWear_StartScan")
public func MetaWear_StartScan() {

    MetaWearController.shared.startScanning()
}

@_cdecl("MetaWear_StopStreaming")
public func MetaWear_StopStreaming() {

    MetaWearController.shared.stopStreaming()
}

@_cdecl("MetaWear_SetUserHeight")
public func MetaWear_SetUserHeight(_ heightCm: Float) {

    MetaWearController.shared.userHeightCm = heightCm
}
