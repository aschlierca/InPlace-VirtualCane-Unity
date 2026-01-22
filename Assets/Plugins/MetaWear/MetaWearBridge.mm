// MetaWearBridge.mm
#include "btle_connection.h"
#include "metawear/core/metawearboard.h"
#include "metawear/sensor/sensor_fusion.h"
#include "metawear/core/datasignal.h"
#include <stdint.h>
#include <stdlib.h>

extern "C" {

// Initialize board with BLE connection
void mw_init_board(MblMwMetaWearBoard* board, MblMwBtleConnection* conn) {
    board->btle_conn = conn;
    init_sensor_fusion_module(board);
}

// Start sensor fusion
void mw_start_sensor_fusion(MblMwMetaWearBoard* board) {
    mbl_mw_sensor_fusion_start(board);
}

// Stop sensor fusion
void mw_stop_sensor_fusion(MblMwMetaWearBoard* board) {
    mbl_mw_sensor_fusion_stop(board);
}

// Get quaternion signal
MblMwDataSignal* mw_get_quaternion_signal(MblMwMetaWearBoard* board) {
    return mbl_mw_sensor_fusion_get_data_signal(board, MBL_MW_SENSOR_FUSION_DATA_QUATERNION);
}

// Subscribe to quaternion data
void mw_subscribe_quaternion(MblMwDataSignal* signal, void* context, MblMwFnData callback) {
    mbl_mw_datasignal_subscribe(signal, context, callback);
    mbl_mw_datasignal_read(signal);
}

// Unsubscribe from any data signal
void mw_unsubscribe(MblMwDataSignal* signal) {
    mbl_mw_datasignal_unsubscribe(signal);
}

// Stop sensor fusion and free module
void mw_free_board(MblMwMetaWearBoard* board) {
    mbl_mw_sensor_fusion_stop(board);
    free_sensor_fusion_module(board);
}

} // extern "C"
