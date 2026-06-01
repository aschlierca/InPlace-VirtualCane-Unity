#pragma once

#ifdef __cplusplus
extern "C"
{
#endif

    void MetaWear_StartScan(void);
    void MetaWear_StopScan(void);
    void MetaWear_StartStreaming(void);
    void MetaWear_StopStreaming(void);
    void MetaWear_Disconnect(void);
    void MetaWear_SetUserHeight(float heightCm);

#ifdef __cplusplus
}
#endif
