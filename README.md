# iOS Build Setup

## Prerequisites

Install CocoaPods if it is not already installed:

```bash
sudo gem install cocoapods
```

## Generate the Xcode Workspace

After building the iOS project from Unity:

1. Open a terminal in the generated Xcode project directory.
2. Initialize CocoaPods:

```bash
pod init
```

3. Replace the contents of the generated `Podfile` with:

```ruby
platform :ios, '14.0'

use_frameworks!

target 'Unity-iPhone' do
  # No pods here — UnityFramework handles them

  target 'Unity-iPhone Tests' do
    inherit! :search_paths
  end
end

target 'UnityFramework' do
  pod 'MetaWear'
end

target 'GameAssembly' do
end

post_install do |installer|
  installer.pods_project.targets.each do |target|
    target.build_configurations.each do |config|

      # Ensure consistent deployment target
      config.build_settings['IPHONEOS_DEPLOYMENT_TARGET'] = '14.0'

      # Required for newer Xcode versions
      config.build_settings['ENABLE_USER_SCRIPT_SANDBOXING'] = 'YES'

    end
  end

  # Fix UnityFramework MetaWear C++ header path
  project_path = 'Unity-iPhone.xcodeproj'
  if File.exist?(project_path)
    project = Xcodeproj::Project.open(project_path)

    project.targets.each do |target|
      if target.name == 'UnityFramework'
        target.build_configurations.each do |config|
          config.build_settings['SWIFT_INCLUDE_PATHS'] = '$(inherited) "${PODS_ROOT}/MetaWear/MetaWear/MetaWear-SDK-Cpp/src"'
        end
      end

      if target.name == 'Unity-iPhone'
        target.build_configurations.each do |config|
          config.build_settings.delete('ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES')
        end
      end
    end

    project.save
  end
end
```

4. Install dependencies:

```bash
pod install
```

## Open the Workspace

Always open the CocoaPods workspace instead of the Xcode project:

```text
Unity-iPhone.xcworkspace
```


## Build and Run

1. Open `Unity-iPhone.xcworkspace`.
2. Select your iOS device as the build target.
3. Configure Signing & Capabilities with your Apple Developer account.
4. Build and run the project from Xcode.
