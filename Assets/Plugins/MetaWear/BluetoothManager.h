#pragma once
#include <stdint.h>

#ifdef __cplusplus
extern "C"
{
#endif

    typedef enum
    {
        MBL_MW_GATT_CHAR_WRITE_WITH_RESPONSE = 0,
        MBL_MW_GATT_CHAR_WRITE_WITHOUT_RESPONSE
    } MblMwGattCharWriteType;

    typedef struct
    {
        uint64_t service_uuid_high;
        uint64_t service_uuid_low;
        uint64_t uuid_high;
        uint64_t uuid_low;
    } MblMwGattChar;

    typedef int32_t (*MblMwFnIntVoidPtrArray)(const void *caller, const uint8_t *start, uint8_t length);
    typedef void (*MblMwFnVoidVoidPtrInt)(const void *caller, int32_t value);

    typedef struct
    {
        void *context;
        void (*write_gatt_char)(void *context, const void *caller, MblMwGattCharWriteType writeType,
                                const MblMwGattChar *characteristic, const uint8_t *value, uint8_t length);
        void (*read_gatt_char)(void *context, const void *caller, const MblMwGattChar *characteristic,
                               MblMwFnIntVoidPtrArray handler);
        void (*enable_notifications)(void *context, const void *caller, const MblMwGattChar *characteristic,
                                     MblMwFnIntVoidPtrArray handler, MblMwFnVoidVoidPtrInt ready);
        void (*on_disconnect)(void *context, const void *caller, MblMwFnVoidVoidPtrInt handler);
    } MblMwBtleConnection;

#ifdef __cplusplus
}
#endif
