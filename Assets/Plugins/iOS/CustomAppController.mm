#import "CustomAppController.h"

@implementation CustomAppController

// Override the scene configuration method
- (UISceneConfiguration *)application:(UIApplication *)application
configurationForConnectingSceneSession:(UISceneSession *)connectingSceneSession
                              options:(UISceneConnectionOptions *)options {
    UISceneConfiguration *configuration = [[UISceneConfiguration alloc] initWithName:@"Default Configuration"
                                                                         sessionRole:connectingSceneSession.role];
    configuration.delegateClass = NSClassFromString(@"UnitySceneDelegate");
    return configuration;
}

- (void)application:(UIApplication *)application
didDiscardSceneSessions:(NSSet<UISceneSession *> *)sceneSessions {
    // Called when the user discards a scene session.
}

@end

// This macro tells Unity to use your custom app controller
IMPL_APP_CONTROLLER_SUBCLASS(CustomAppController)
