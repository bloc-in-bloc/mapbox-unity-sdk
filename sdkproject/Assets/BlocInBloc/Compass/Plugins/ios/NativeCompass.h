//
//  NativeCompass.h
//
//  Created by BlocInBloc on 20/01/2020.
//  Copyright © 2020 BlocInBloc. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <CoreLocation/CoreLocation.h>

@interface NativeCompass : NSObject <CLLocationManagerDelegate>
{
    @public CLHeading *lastHeading;
    @public CLLocation *lastLocation;
    @public NSString *lastLocationManagerStatus;
    @public NSString *errorMsg;

    @public bool headingAvailable;
    @public bool locationAvailable;
}

+ (char*) convertNSStringToCString:(NSString*) nsString;

- (bool)initLocationAndHeading;
- (bool)requestAuthorizationNeeded;
- (void)startUpdate;
- (void)stopUpdate;

- (void)orientationChanged:(NSNotification *)note;
- (void)setHeadingOrientation:(UIDeviceOrientation)deviceOrientation;
@end
