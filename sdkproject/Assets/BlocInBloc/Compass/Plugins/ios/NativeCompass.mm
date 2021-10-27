//
//  NativeCompass.mm
//
//  Created by BlocInBloc on 20/01/2020.
//  Copyright © 2020 BlocInBloc. All rights reserved.
//

#import "NativeCompass.h"
#import <UIKit/UIKit.h>
#import "UnityInterface.h"

NativeCompass *nativeCompass;

extern "C" {
bool InitLocationManager()
{
    nativeCompass = [[NativeCompass alloc] init];
    return [nativeCompass initLocationAndHeading];
}

bool RequestAuthorizationNeeded()
{
    if (nativeCompass != nil) {
        return [nativeCompass requestAuthorizationNeeded];
    } else {
        NSLog(@"Error: nativeCompass not initialized");
        return false;
    }
}

void StartUpdate()
{
    if (nativeCompass != nil) {
        [nativeCompass startUpdate];
    } else {
        NSLog(@"Error: nativeCompass not initialized");
    }
}

void StopUpdate()
{
    if (nativeCompass != nil) {
        [nativeCompass stopUpdate];
    } else {
        NSLog(@"Error: nativeCompass not initialized");
    }
}

char * GetErrorMsg()
{
    if (nativeCompass == nil || nativeCompass->errorMsg == nil) {
        return NULL;
    }
    return [NativeCompass convertNSStringToCString:nativeCompass->errorMsg];
}

//MARK: Heading
bool GetHeadingAvailable()
{
    return nativeCompass != nil && nativeCompass->headingAvailable && nativeCompass->lastHeading != nil;
}

float GetMagneticHeading()
{
    if (nativeCompass == nil || nativeCompass->lastHeading == nil) {
        return -1;
    }
    return nativeCompass->lastHeading.magneticHeading;
}

float GetTrueHeading()
{
    if (nativeCompass == nil || nativeCompass->lastHeading == nil) {
        return -1;
    }
    return nativeCompass->lastHeading.trueHeading;
}

float GetHeadingAccuracy()
{
    if (nativeCompass == nil || nativeCompass->lastHeading == nil) {
        return -1;
    }
    return nativeCompass->lastHeading.headingAccuracy;
}

double GetHeadingTimestamp()
{
    if (nativeCompass == nil || nativeCompass->lastHeading == nil) {
        return -1;
    }
    return nativeCompass->lastHeading.timestamp.timeIntervalSince1970;
}

float GetMagneticField()
{
    return sqrt(nativeCompass->lastHeading.x * nativeCompass->lastHeading.x + nativeCompass->lastHeading.y * nativeCompass->lastHeading.y + nativeCompass->lastHeading.z * nativeCompass->lastHeading.z);
}

//MARK: Location

bool GetLocationAvailable()
{
    return nativeCompass != nil && nativeCompass->locationAvailable && nativeCompass->lastLocation != nil;
}

double GetLocationLatitude()
{
    if (nativeCompass == nil || nativeCompass->lastLocation == nil) {
        return 0;
    }
    return nativeCompass->lastLocation.coordinate.latitude;
}

double GetLocationLongitude()
{
    if (nativeCompass == nil || nativeCompass->lastLocation == nil) {
        return -1;
    }
    return nativeCompass->lastLocation.coordinate.longitude;
}

double GetLocationAltitude()
{
    if (nativeCompass == nil || nativeCompass->lastLocation == nil) {
        return -1;
    }
    return nativeCompass->lastLocation.altitude;
}

float GetLocationHorizontalAccuracy()
{
    if (nativeCompass == nil || nativeCompass->lastLocation == nil) {
        return -1;
    }
    return nativeCompass->lastLocation.horizontalAccuracy;
}

double GetLocationTimestamp()
{
    if (nativeCompass == nil || nativeCompass->lastLocation == nil) {
        return -1;
    }
    return nativeCompass->lastLocation.timestamp.timeIntervalSince1970;
}

// Values : NotInitialized, Initializing, Starting, Failed, Unauthorized, Authorized, Running, Stopped
char * GetLocationStatus()
{
    NSString *status = nativeCompass->lastLocationManagerStatus;
    if (nativeCompass == nil || nativeCompass->lastLocationManagerStatus == nil) {
        status = @"NotInitialized";
    }
    return [NativeCompass convertNSStringToCString:status];
}

float GetCourseAccuracy()
{
    if (nativeCompass == nil || nativeCompass->lastLocation == nil) {
        return -1;
    }
    return nativeCompass->lastLocation.courseAccuracy;
}

float GetCourse()
{
    if (nativeCompass == nil || nativeCompass->lastLocation == nil) {
        return -1;
    }
    return nativeCompass->lastLocation.course;
}
}

@implementation NativeCompass : NSObject

//@ObservedObject var orientationInfo = OrientationInfo()
//var orientationListener: AnyCancellable? = nil

CLLocationManager *_locationManager;

- (instancetype)init
{
    self = [super init];
    if (self) {
        lastHeading = nil;
        lastLocation = nil;
        errorMsg = nil;
        headingAvailable = false;
        locationAvailable = false;
        lastLocationManagerStatus = @"NotInitialized";
    }
    return self;
}

- (bool)initLocationAndHeading {
    if (![CLLocationManager locationServicesEnabled] || ![CLLocationManager headingAvailable]) {
        errorMsg = [NSString stringWithFormat:@"Error: Location service is %@, Heading service is %@",([CLLocationManager locationServicesEnabled] ? @"available" : @"unavailable"), ([CLLocationManager headingAvailable] ? @"available" : @"unavailable")];
        NSLog(errorMsg);
        return false;
    }
    _locationManager = [[CLLocationManager alloc] init];

    [self setHeadingOrientationWithInterfaceOrientation:UIApplication.sharedApplication.statusBarOrientation];
    [self listenDeviceOrientation];
    _locationManager.delegate = self;
    _locationManager.desiredAccuracy = kCLLocationAccuracyBestForNavigation;

    lastLocationManagerStatus = @"Initialized";

    return true;
}

- (bool)requestAuthorizationNeeded {
    if (@available(iOS 14.0, *)) {
        return _locationManager.authorizationStatus == kCLAuthorizationStatusNotDetermined;
    } else {
        return [CLLocationManager authorizationStatus] == kCLAuthorizationStatusNotDetermined;
    }
}

- (void)startUpdate {
    lastLocationManagerStatus = @"Starting";
    if ([self requestAuthorizationNeeded]) {
        [_locationManager requestWhenInUseAuthorization];
    }

    [_locationManager startUpdatingHeading];
    [_locationManager startUpdatingLocation];
}

- (void)stopUpdate {
    [_locationManager stopUpdatingHeading];
    [_locationManager stopUpdatingLocation];
    lastHeading = nil;
    lastLocation = nil;
    headingAvailable = false;
    locationAvailable = false;
    lastLocationManagerStatus = @"Stopped";
}

+ (char *)convertNSStringToCString:(NSString *)nsString {
    if (nsString == NULL) return NULL;

    const char *nsStringUtf8 = [nsString UTF8String];
    //create a null terminated C string on the heap so that our string's memory isn't wiped out right after method's return
    char *cString = (char *)malloc(strlen(nsStringUtf8) + 1);
    strcpy(cString, nsStringUtf8);

    return cString;
}

//MARK: CLLocationManagerDelegate implementations

// Allow system to show calibration overlay if needed
- (BOOL)locationManagerShouldDisplayHeadingCalibration:(CLLocationManager *)manager {
    return true;
}

- (void)locationManager:(CLLocationManager *)manager didUpdateHeading:(CLHeading *)newHeading {
    headingAvailable = newHeading.headingAccuracy > 0 ? true : false;
    lastHeading = newHeading;
}

- (void)locationManager:(CLLocationManager *)manager didUpdateLocations:(NSArray<CLLocation *> *)locations {
    locationAvailable = true;
    lastLocation = locations.lastObject;
    lastLocationManagerStatus = @"Running";
}

- (void)locationManager:(CLLocationManager *)manager didFailWithError:(NSError *)error {
    errorMsg = error.localizedDescription;
    headingAvailable = false;
    locationAvailable = false;
    lastLocationManagerStatus = @"Failed";
}

- (void)locationManagerDidPauseLocationUpdates:(CLLocationManager *)manager {
    lastLocationManagerStatus = @"Stopped";
}

- (void)locationManagerDidResumeLocationUpdates:(CLLocationManager *)manager {
    lastLocationManagerStatus = @"Running";
}

- (void)locationManager:(CLLocationManager *)manager didChangeAuthorizationStatus:(CLAuthorizationStatus)status {
    // UnitySendMessage("BIBNativeBridge", "OnLocationAuthorizationStatusChange", [[self authorizationStatusToString: status] UTF8String]);
    // Not determined to prevent error message at first call
    if (status != kCLAuthorizationStatusNotDetermined && status != kCLAuthorizationStatusAuthorizedWhenInUse && status != kCLAuthorizationStatusAuthorizedAlways) {
        errorMsg = @"You should accept location access";
        headingAvailable = false;
        locationAvailable = false;
        lastLocationManagerStatus = @"Unauthorized";
    } else {
        errorMsg = nil;
        lastLocationManagerStatus = @"Authorized";
    }
}

//MARK: UIDeviceOrientation listener

- (void)listenDeviceOrientation {
    [[UIDevice currentDevice] beginGeneratingDeviceOrientationNotifications];

    [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(orientationChanged:) name:UIDeviceOrientationDidChangeNotification object:[UIDevice currentDevice]];
}

- (void)orientationChanged:(NSNotification *)note {
    [self setHeadingOrientationWithDeviceOrientation:UIDevice.currentDevice.orientation];
}

- (void)setHeadingOrientationWithInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation {
    switch (interfaceOrientation) {
        case UIInterfaceOrientationPortrait:
            _locationManager.headingOrientation = CLDeviceOrientation::CLDeviceOrientationPortrait;
            break;
        case UIInterfaceOrientationPortraitUpsideDown:
            _locationManager.headingOrientation = CLDeviceOrientation::CLDeviceOrientationPortraitUpsideDown;
            break;
        case UIInterfaceOrientationLandscapeLeft:
            _locationManager.headingOrientation = CLDeviceOrientation::CLDeviceOrientationLandscapeLeft;
            break;
        case UIInterfaceOrientationLandscapeRight:
            _locationManager.headingOrientation = CLDeviceOrientation::CLDeviceOrientationLandscapeRight;
            break;
        case UIInterfaceOrientationUnknown:
            _locationManager.headingOrientation = CLDeviceOrientation::CLDeviceOrientationUnknown;
            break;
        default:
            break;
    }
}

- (void)setHeadingOrientationWithDeviceOrientation:(UIDeviceOrientation)deviceOrientation {
    switch (deviceOrientation) {
        case UIDeviceOrientationPortrait:
            _locationManager.headingOrientation = CLDeviceOrientation::CLDeviceOrientationPortrait;
            break;
        case UIDeviceOrientationPortraitUpsideDown:
            _locationManager.headingOrientation = CLDeviceOrientation::CLDeviceOrientationPortraitUpsideDown;
            break;
        case UIDeviceOrientationLandscapeLeft:
            _locationManager.headingOrientation = CLDeviceOrientation::CLDeviceOrientationLandscapeLeft;
            break;
        case UIDeviceOrientationLandscapeRight:
            _locationManager.headingOrientation = CLDeviceOrientation::CLDeviceOrientationLandscapeRight;
            break;
        case UIDeviceOrientationUnknown:
            _locationManager.headingOrientation = CLDeviceOrientation::CLDeviceOrientationUnknown;
            break;
        default:
            break;
    }
}

- (NSString*)authorizationStatusToString:(CLAuthorizationStatus)authorizationStatus{
    NSString *result = nil;
    switch(authorizationStatus) {
        case kCLAuthorizationStatusRestricted:
            result = @"kCLAuthorizationStatusRestricted";
            break;
        case kCLAuthorizationStatusDenied:
            result = @"kCLAuthorizationStatusDenied";
            break;
        case kCLAuthorizationStatusAuthorizedAlways:
            result = @"kCLAuthorizationStatusAuthorizedAlways";
            break;
        case kCLAuthorizationStatusAuthorizedWhenInUse:
            result = @"kCLAuthorizationStatusAuthorizedWhenInUse";
            break;
        default:
            result =@"kCLAuthorizationStatusNotDetermined";
            break;
    }
    return result;
}

@end
