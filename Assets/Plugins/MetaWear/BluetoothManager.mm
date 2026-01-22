#import "BluetoothManager.h"
#import <CoreBluetooth/CoreBluetooth.h>

@interface BLEDelegate : NSObject<CBCentralManagerDelegate, CBPeripheralDelegate>
@property (nonatomic, strong) CBCentralManager* central;
@property (nonatomic, strong) CBPeripheral* peripheral;
@property (nonatomic) MblMwBtleConnection* connection;
@end

@implementation BLEDelegate
- (instancetype)initWithConnection:(MblMwBtleConnection*)conn {
    self = [super init];
    if(self) {
        _connection = conn;
        _central = [[CBCentralManager alloc] initWithDelegate:self queue:nil];
    }
    return self;
}

- (void)centralManagerDidUpdateState:(CBCentralManager *)central {
    if (central.state == CBManagerStatePoweredOn) {
        [central scanForPeripheralsWithServices:nil options:nil];
    }
}

// Discover peripheral
- (void)centralManager:(CBCentralManager *)central didDiscoverPeripheral:(CBPeripheral *)peripheral
    advertisementData:(NSDictionary<NSString *,id> *)advertisementData RSSI:(NSNumber *)RSSI {
    self.peripheral = peripheral;
    self.peripheral.delegate = self;
    [self.central stopScan];
    [self.central connectPeripheral:peripheral options:nil];
}

// Connected
- (void)centralManager:(CBCentralManager *)central didConnectPeripheral:(CBPeripheral *)peripheral {
    [peripheral discoverServices:nil];
}

// Discover services
- (void)peripheral:(CBPeripheral *)peripheral didDiscoverServices:(NSError *)error {
    for (CBService* service in peripheral.services) {
        [peripheral discoverCharacteristics:nil forService:service];
    }
}

// Discover characteristics
- (void)peripheral:(CBPeripheral *)peripheral didDiscoverCharacteristicsForService:(CBService *)service error:(NSError *)error {
    // You can match characteristics using MblMwGattChar
}

// Write value
- (void)writeCharacteristic:(CBCharacteristic*)characteristic data:(NSData*)data withResponse:(BOOL)response {
    [self.peripheral writeValue:data forCharacteristic:characteristic type:response ? CBCharacteristicWriteWithResponse : CBCharacteristicWriteWithoutResponse];
}

// Read value
- (void)readCharacteristic:(CBCharacteristic*)characteristic handler:(MblMwFnIntVoidPtrArray)handler {
    [self.peripheral readValueForCharacteristic:characteristic];
}

// Enable notifications
- (void)setNotify:(CBCharacteristic*)characteristic handler:(MblMwFnIntVoidPtrArray)handler {
    [self.peripheral setNotifyValue:YES forCharacteristic:characteristic];
}

// Disconnect
- (void)centralManager:(CBCentralManager *)central didDisconnectPeripheral:(CBPeripheral *)peripheral error:(NSError *)error {
    if(self.connection && self.connection->on_disconnect) {
        self.connection->on_disconnect(self.connection->context, NULL, 0);
    }
}

@end

// Bridge functions
void write_gatt_char(void *context, const void* caller, MblMwGattCharWriteType writeType,
                     const MblMwGattChar* characteristic, const uint8_t* value, uint8_t length) {
    // Convert value -> NSData and call BLEDelegate
}

void read_gatt_char(void *context, const void* caller, const MblMwGattChar* characteristic, MblMwFnIntVoidPtrArray handler) {}
void enable_notifications(void *context, const void* caller, const MblMwGattChar* characteristic, MblMwFnIntVoidPtrArray handler, MblMwFnVoidVoidPtrInt ready) {}
void on_disconnect(void *context, const void* caller, MblMwFnVoidVoidPtrInt handler) {}

MblMwBtleConnection* create_connection() {
    MblMwBtleConnection* conn = malloc(sizeof(MblMwBtleConnection));
    conn->context = conn;
    conn->write_gatt_char = write_gatt_char;
    conn->read_gatt_char = read_gatt_char;
    conn->enable_notifications = enable_notifications;
    conn->on_disconnect = on_disconnect;
    return conn;
}
