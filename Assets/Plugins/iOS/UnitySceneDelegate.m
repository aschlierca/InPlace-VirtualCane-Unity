#import "UnitySceneDelegate.h"
#import "UnityAppController.h"

@implementation UnitySceneDelegate

- (void)scene:(UIScene *)scene willConnectToSession:(UISceneSession *)session options:(UISceneConnectionOptions *)connectionOptions {
    // Ensure we have a window scene
    if (![scene isKindOfClass:[UIWindowScene class]]) {
        return;
    }

    UIWindowScene *windowScene = (UIWindowScene *)scene;

    // Get the Unity app controller
    UnityAppController *appController = GetAppController();

    // If the app controller already has a window, update its window scene
    if (appController.window) {
        // The window was already created by UnityAppController
        // Just make sure it's associated with this scene
        if (!appController.window.windowScene) {
            // Create a new window with the proper scene
            UIWindow *newWindow = [[UIWindow alloc] initWithWindowScene:windowScene];
            newWindow.rootViewController = appController.window.rootViewController;
            [newWindow makeKeyAndVisible];

            // Replace the old window
            [appController setWindow:newWindow];
        }
    }
}

- (void)sceneDidDisconnect:(UIScene *)scene {
    // Called as the scene is being released by the system.
}

- (void)sceneDidBecomeActive:(UIScene *)scene {
    // Called when the scene has moved from an inactive state to an active state.
    UnityAppController *appController = GetAppController();
    if (appController) {
        [[UIApplication sharedApplication].delegate applicationDidBecomeActive:[UIApplication sharedApplication]];
    }
}

- (void)sceneWillResignActive:(UIScene *)scene {
    // Called when the scene will move from an active state to an inactive state.
    UnityAppController *appController = GetAppController();
    if (appController) {
        [[UIApplication sharedApplication].delegate applicationWillResignActive:[UIApplication sharedApplication]];
    }
}

- (void)sceneWillEnterForeground:(UIScene *)scene {
    // Called as the scene transitions from the background to the foreground.
    UnityAppController *appController = GetAppController();
    if (appController) {
        [[UIApplication sharedApplication].delegate applicationWillEnterForeground:[UIApplication sharedApplication]];
    }
}

- (void)sceneDidEnterBackground:(UIScene *)scene {
    // Called as the scene transitions from the foreground to the background.
    UnityAppController *appController = GetAppController();
    if (appController) {
        [[UIApplication sharedApplication].delegate applicationDidEnterBackground:[UIApplication sharedApplication]];
    }
}

@end
