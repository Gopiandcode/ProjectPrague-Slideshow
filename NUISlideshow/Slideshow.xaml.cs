using Microsoft.Gestures;
using Microsoft.Gestures.Endpoint;
using Microsoft.Gestures.InputBindings;
using Newtonsoft.Json;
using NUISlideshow.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NUISlideshow
{

    /// Interaction logic for Slideshow.xaml

    public partial class Slideshow : Window
    {
        MediaList medialist;
        private Window parent;
        GesturesServiceEndpoint service;

        public Slideshow(Window parent, string url)
        {
            this.parent = parent;
            this.Closed += (o, a) => { DisconnectService(); };
            this.Closed += Slideshow_Closed;
            medialist = new MediaList();
            ConnectService();

            InitializeComponent();

            // Each time the medialist updates, update the Slideshow image source
            medialist.addListener((Object sender, PropertyChangedEventArgs args) => {
                Dispatcher.Invoke(new Action(() => { 
                MainImage.Source = medialist.CurrentImage;
                }));
            });

            GenerateImages(url).ContinueWith((result) =>
            {
                if (result.Result == null || result.Result.Count == 0)
                {
                    MessageBoxButton button = MessageBoxButton.OK;

                    MessageBoxResult res = MessageBox.Show("Unfortunately we couldn't find any image urls (ending in .png, .jpg, etc...)" +
                        " at the specified subreddit.", "Error - 404 Not Found", button);
                    if (res == MessageBoxResult.OK)
                    {
                        Dispatcher.Invoke(new Action(() => {
                            this.Close();
                            parent.Show();
                        }));
                    }
                }
                else foreach (BitmapImage img in result.Result)
                {
                    medialist.Add(img);
                }

            });
            
        }


        public async void ConnectService()
        {
            service = GesturesServiceEndpointFactory.Create();
            await service.ConnectAsync();

            // Define Gestures + Triggers Below

            var open_hand_Start = new HandPose("start", // <-- Whenever this Handpose is seen, the GestureService will display this as an identifier. 
                
                                                new FingerPose(new[] { // Target all the fingers,
                                                                    Finger.Index,
                                                                    Finger.Middle,
                                                                    Finger.Pinky,
                                                                    Finger.Ring
                                                                }, // With fingers outstretched, pointing forward.
                                                                FingerFlexion.OpenStretched, 
                                                                PoseDirection.Forward
                                                                )
                                                );
            var closed_hand_end = new HandPose("end",  // <-- Whenever this Handpose is seen, the GestureService will display this as an identifier. 

                                                new FingerPose(
                                                                new[] { // Target all the fingers,
                                                                        Finger.Index,
                                                                        Finger.Middle,
                                                                        Finger.Pinky,
                                                                        Finger.Ring
                                                                },   // With fingers closed
                                                                FingerFlexion.OpenStretched
                                                               ),
                                                                new PalmPose(new AnyHandContext(), PoseDirection.Backward)
                                                );
         
            var swipe_left_guesture = new Gesture("SwipeLeft",                  // <-- Whenever this Gesture is seen, the GestureService will display this as an identifier. 

                                                  open_hand_Start,              // The gesture begins with an open hand
                
                                                  new HandMotion("motionLeft",  // <-- Whenever this HandMotion is seen, the GestureService will display this as an identifier.
                                                                  
                                                                 HorizontalMotionSegment.Left  // A predefined MotionSegment of motion to the left
                                                                 ),
                                                  closed_hand_end               // After the leftward motion, the palm must be facing away
                                                   );

            open_hand_Start = new HandPose("start",  // <-- Whenever this Handpose is seen, the GestureService will display this as an identifier. 
                                            new FingerPose(new[] { Finger.Index, Finger.Middle, Finger.Pinky, Finger.Ring },  // Target all the fingers,
                                            FingerFlexion.OpenStretched,   // With fingers outstretched, pointing forward.
                                            PoseDirection.Forward));

            closed_hand_end = new HandPose("end",  // <-- Whenever this Handpose is seen, the GestureService will display this as an identifier. 
                                           new FingerPose(
                                                    new[] { // Target all the fingers,
                                                                        Finger.Index,
                                                                        Finger.Middle,
                                                                        Finger.Pinky,
                                                                        Finger.Ring
                                                    },   // With fingers closed
                                                    FingerFlexion.OpenStretched
                                                   ),
                                                    new PalmPose(new AnyHandContext(), PoseDirection.Forward)
                                    );
            
            var swipe_right_guesture = new Gesture("SwipeRight",                  // <-- Whenever this Gesture is seen, the GestureService will display this as an identifier.

                                                   open_hand_Start,               // The gesture begins with an open hand 

                                                   new HandMotion("motionRight", HorizontalMotionSegment.Right),  // A predefined MotionSegment of motion to the left
                                                   closed_hand_end                // After the leftward motion, the hand must be facing towards the camera
                                                   );

            // Set the callbacks to run on the detection of these triggers
            swipe_left_guesture.Triggered += (object sender, GestureSegmentTriggeredEventArgs e) => { Dispatcher.Invoke(new Action(() => {
                if (medialist.Count != 0)
                    medialist.decrementPosition();
            })); };
            swipe_right_guesture.Triggered += (object sender, GestureSegmentTriggeredEventArgs e) => { Dispatcher.Invoke(new Action(() => {
                if (medialist.Count != 0)
                    medialist.incrementPosition();
            })); };

            // Based off the hand pose on the Project Prague Page - https://docs.microsoft.com/en-us/gestures/

            var rotate_left_begin = new HandPose("RotateBegin", new FingerPose(
                                                                               new[] { Finger.Thumb, Finger.Index }, // Target the Thumb and Index.
                                                                               FingerFlexion.Open // Both fingers pointing forwards
                                                                               ),
                                                                               new FingerPose(
                                                                               new[] { Finger.Ring, Finger.Middle, Finger.Pinky }, // Target the Rest of the fingers.
                                                                               FingerFlexion.Open, PoseDirection.Up // Both fingers pointing upwards
                                                                               ),
                                                                new FingertipPlacementRelation(Finger.Index, RelativePlacement.Above, Finger.Thumb), // The Index should be above the thumb
                                                                new FingertipDistanceRelation(Finger.Index, RelativeDistance.Touching, Finger.Thumb) // Both fingers should be touching
                                                 );

            var rotate_left_end = new HandPose("RotateEnd", new FingerPose(
                                                                            new[] { Finger.Thumb, Finger.Index }, // Target the thumb and Index
                                                                            FingerFlexion.Open 
                                                                           ),
                                                                           new FingerPose(
                                                                               new[] { Finger.Ring, Finger.Middle, Finger.Pinky }, // Target the Rest of the fingers.
                                                                               FingerFlexion.Open, PoseDirection.Left // Both fingers pointing to the Left
                                                                               ),
                                                           new FingertipDistanceRelation(Finger.Index, RelativeDistance.Touching, Finger.Thumb), // Both fingers should still be touching.
                                                           new FingertipPlacementRelation(Finger.Index, RelativePlacement.Left, Finger.Thumb)    // The index finger should be to the left of the thumm
                                                           );

            var rotate_left_gesture = new Gesture("rotateLeft", rotate_left_begin, rotate_left_end);


            var rotate_right_begin = new HandPose("RotateBegin", new FingerPose(
                                                                               new[] { Finger.Thumb, Finger.Index }, // Target the Thumb and Index.
                                                                               FingerFlexion.Open
                                                                               ),
                                                                 new FingerPose(
                                                                               new[] { Finger.Ring, Finger.Middle, Finger.Pinky }, // Target the Rest of the fingers.
                                                                               FingerFlexion.Open, PoseDirection.Up // Both fingers pointing upwards
                                                                               ),
                                                                new FingertipPlacementRelation(Finger.Index, RelativePlacement.Above, Finger.Thumb), // The Index should be above the thumb
                                                                new FingertipDistanceRelation(Finger.Index, RelativeDistance.Touching, Finger.Thumb) // Both fingers should be touching
                                                 );

            var rotate_right_end = new HandPose("RotateEnd", new FingerPose(
                                                                            new[] { Finger.Thumb, Finger.Index }, // Target the thumb and Index
                                                                            FingerFlexion.Open
                                                                           ),
                                                             new FingerPose(
                                                                               new[] { Finger.Ring, Finger.Middle, Finger.Pinky }, // Target the Rest of the fingers.
                                                                               FingerFlexion.Open, PoseDirection.Right // Both fingers pointing to the left
                                                                               ),
                                                           new FingertipDistanceRelation(Finger.Index, RelativeDistance.Touching, Finger.Thumb), // Both fingers should still be touching.
                                                           new FingertipPlacementRelation(Finger.Index, RelativePlacement.Right, Finger.Thumb)    // The index finger should be to the Right of the thumm
                                                           );

            var rotate_right_gesture = new Gesture("rotateRight", rotate_right_begin, rotate_right_end);
            rotate_left_gesture.Triggered += (object sender, GestureSegmentTriggeredEventArgs e) => { rotateImage90DegLeft();};

            rotate_right_gesture.Triggered += (object sender, GestureSegmentTriggeredEventArgs e) => { rotateImage90DegRight();};

            await service.RegisterGesture(rotate_left_gesture);
            await service.RegisterGesture(rotate_right_gesture);



            //  Register the gestures with the service
            await service.RegisterGesture(swipe_left_guesture);
            await service.RegisterGesture(swipe_right_guesture);

        }



        public async void DisconnectService()
        {
            await service?.Disconnect();
        }



        public async Task<List<BitmapImage>> GenerateImages(string url)
        {
            Regex image_expr = new Regex(@".(?:jpg|jpeg|png|bmp|gif|gifv)$", RegexOptions.Compiled);
            Regex gifv_expr = new Regex(@".(?:gifv)$", RegexOptions.Compiled);
            
            // Generate the full api request link from the url.
            url = "https://www.reddit.com/" + url + ".json?limit=100";

            List<BitmapImage> images = new List<BitmapImage>();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    if(response.IsSuccessStatusCode)
                    {
                        try
                        {
                            string result = await response.Content.ReadAsStringAsync();
                            var JSONroot = JsonConvert.DeserializeObject<Rootobject>(result);

                            foreach(var child in JSONroot.data.children)
                            {
                                // If the url actually points to a compatible image format
                                if(image_expr.IsMatch(child.data.url))
                                {
                                    BitmapImage img = new BitmapImage(new Uri(child.data.url));
                                    images.Add(img);
                                }
                            }

                            return images;

                        } catch(UriFormatException e)
                        {
                            Debug.WriteLine(e);
                            return images;
                        }
                    } else
                    {
                        return images;
                    }
                }
            } catch(HttpRequestException e)
            {
                // If it fails, return an empty list.
                return images;
            }
        }


        private void rotateImage90DegRight()
        {
            Dispatcher.Invoke(new Action(() => {
                BitmapImage Original = MainImage.Source as BitmapImage;
            BitmapImage Rotated = new BitmapImage();
            Rotated.BeginInit();
            Rotated.UriSource = Original.UriSource;
            Rotated.Rotation = Rotation.Rotate90;
            Rotated.EndInit();

            MainImage.Source = Rotated;
        }));

        }

        private void rotateImage90DegLeft()
        {
            Dispatcher.Invoke(new Action(() => { 
            BitmapImage Original = MainImage.Source as BitmapImage;
            BitmapImage Rotated = new BitmapImage();
            Rotated.BeginInit();
            Rotated.UriSource = Original.UriSource;
            Rotated.Rotation = Rotation.Rotate270;
            Rotated.EndInit();

            MainImage.Source = Rotated;
            }));

        }


        private void Slideshow_Closed(object sender, EventArgs e)
        {
            parent.Show();
        }

        private void PreviousImage(object sender, RoutedEventArgs e)
        {
            if(medialist.Count != 0)
                medialist.decrementPosition();
        }

        private void NextImage(object sender, RoutedEventArgs e)
        {
            if(medialist.Count != 0)
                medialist.incrementPosition();
        }

        private void GoBackButton(object sender, RoutedEventArgs e)
        {
            this.Close();
            parent.Show();
        }

    }
}
