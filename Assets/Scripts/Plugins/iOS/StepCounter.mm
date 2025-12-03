#import <Foundation/Foundation.h>
#import <CoreMotion/CoreMotion.h>

static CMPedometer *pedometer;
static int stepCount = 0;

extern "C" {

void StartStepCounter() {
    if (!pedometer) {
        pedometer = [[CMPedometer alloc] init];
    }

    if ([CMPedometer isStepCountingAvailable]) {
        [pedometer startPedometerUpdatesFromDate:[NSDate date]
                                      withHandler:^(CMPedometerData *data, NSError *error) {
            if (!error) {
                stepCount = data.numberOfSteps.intValue;
            }
        }];
    }
}

int GetStepCount() {
    return stepCount;
}

}
