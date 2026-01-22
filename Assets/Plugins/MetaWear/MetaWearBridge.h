#pragma once
#include "metawear/src/metawear/platform/btle_connection.h"
#include "metawear/src/metawear/core/metawearboard.h"

#ifdef __cplusplus
extern "C"
{
#endif

    void mw_init_board(MblMwMetaWearBoard *board, MblMwBtleConnection *btle);
    void mw_start_sensor_fusion(MblMwMetaWearBoard *board);
    void mw_stop_sensor_fusion(MblMwMetaWearBoard *board);
    MblMwDataSignal *mw_get_quaternion(MblMwMetaWearBoard *board);

#ifdef __cplusplus
}
#endif
