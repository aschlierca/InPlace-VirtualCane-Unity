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
        // Higher new-sample weight = the filter reacts to a footfall within a
        // frame or two instead of ramping up over many frames. This removes the
        // detection lag (which was being perceived as audio lag downstream).
        filteredMagnitude = 0.6 * filteredMagnitude + 0.4 * magnitude;

        // threshold + cooldown
        // Threshold raised to compensate for the more reactive (noisier) filter.
        double threshold = 1.45; // increase if it now counts extra steps
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
