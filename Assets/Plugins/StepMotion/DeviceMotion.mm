#import <CoreMotion/CoreMotion.h>

static CMMotionManager *motionManager;
static double pitch, roll, yaw;

extern "C" {

void StartDeviceMotion() {
    if (!motionManager) {
        motionManager = [[CMMotionManager alloc] init];
    }

    if (motionManager.deviceMotionAvailable) {
        [motionManager startDeviceMotionUpdates];

        // Update at 60 Hz
        motionManager.deviceMotionUpdateInterval = 1.0 / 60.0;
    }
}

double GetPitch() { return motionManager.deviceMotion.attitude.pitch; }
double GetRoll()  { return motionManager.deviceMotion.attitude.roll; }
double GetYaw()   { return motionManager.deviceMotion.attitude.yaw; }

}
