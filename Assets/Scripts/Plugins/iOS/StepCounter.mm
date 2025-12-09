#import <CoreMotion/CoreMotion.h>

static CMMotionManager *motionManager;
static int stepCount = 0;
static double lastPeakTime = 0;
static double filteredMagnitude = 1.0; // low-pass filtered value

extern "C" {

void StartStepCounter() {
    if (!motionManager) {
        motionManager = [[CMMotionManager alloc] init];
    }

    motionManager.accelerometerUpdateInterval = 1.0 / 60.0;

    [motionManager startAccelerometerUpdatesToQueue:[NSOperationQueue currentQueue]
                                         withHandler:^(CMAccelerometerData *data, NSError *error) {

        if (error) return;

        double x = data.acceleration.x;
        double y = data.acceleration.y;
        double z = data.acceleration.z;

        // acceleration magnitude
        double magnitude = sqrt(x*x + y*y + z*z);

        // smooth the signal (low-pass filter)
        filteredMagnitude = 0.9 * filteredMagnitude + 0.1 * magnitude;

        // threshold + cooldown
        double threshold = 1.35; // increase if still too sensitive
        double minInterval = 0.45; // seconds between steps

        double now = CACurrentMediaTime();
        if (filteredMagnitude > threshold && (now - lastPeakTime) > minInterval) {
            stepCount++;
            lastPeakTime = now;
        }
    }];
}

int GetStepCount() {
    return stepCount;
}

}
