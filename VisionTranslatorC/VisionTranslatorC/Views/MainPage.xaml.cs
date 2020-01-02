using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Acr.UserDialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Plugin.Media;
using Plugin.Media.Abstractions;
using VisionTranslatorC.Models;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace VisionTranslatorC.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        private static readonly string subscriptionkey="42803824df354787adfd827f9c4b8314";
        private static readonly string translatorkey="925daa129d634acb926ff3714ebdf0a7";

        private static string translatorendpoint="https://api.cognitive.microsofttranslator.com";

        private static readonly string apiendpoint="https://northeurope.api.cognitive.microsoft.com/vision/v2.1/ocr";
        private string _totranslate;
        string translatedphrase;
        private Stream camerasStream;

        public MainPage ()
        {
            InitializeComponent ();
        }

        private async void Capture_OnClicked(object sender, EventArgs e)
        {
            var photo=await CrossMedia.Current.TakePhotoAsync(
                new StoreCameraMediaOptions {PhotoSize=PhotoSize.Small});

            if (photo != null)
                Result.Source=ImageSource.FromStream(() =>
                {
                    camerasStream=photo.GetStream ();
                    return photo.GetStream ();
                });
        }

        private async void Analyse_OnClicked(object sender, EventArgs e)
        {
            try
            {
                UserDialogs.Instance.ShowLoading("Analyse Image",MaskType.Gradient);
                var client=new HttpClient ();
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionkey);
                var requestParameters="language=unk&detectOrientation=true";
                var uri=apiendpoint + "?" + requestParameters;
                var byteData=GetImageAsByteArray(Result.Source);
                HttpResponseMessage response;
                using (var content=new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType=
                        new MediaTypeHeaderValue("application/octet-stream");

                    // Asynchronously call the REST API method.
                    response=await client.PostAsync(uri, content);
                }

                // Asynchronously get the JSON response.
                var contentString=await response.Content.ReadAsStringAsync ();

                // Display the JSON response.
                var finalstring=new StringBuilder ();
                var finaljson=JToken.Parse(contentString).ToString ();
                var posdef=JsonConvert.DeserializeObject<RootObject>(finaljson);
                var wordsList=new List<string> ();
               
               
                foreach (var i in posdef.regions)
                foreach (var j in i.lines)
                foreach (var k in j.words)
                {
                    finalstring.Append(k.text + " ");
                    wordsList.Add(k.text);
                }

                final.Text=finalstring.ToString ();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
            UserDialogs.Instance.HideLoading ();
        }

        private byte[] GetImageAsByteArray(ImageSource resultSource)
        {
            var binaryReader=new BinaryReader(camerasStream);
            return binaryReader.ReadBytes((int) camerasStream.Length);
        }

        private void Lang_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            _totranslate=lang.SelectedItem.ToString ();
        }

        private void Translate_OnClicked(object sender, EventArgs e)
        {
            UserDialogs.Instance.ShowLoading("Translating");
            switch (_totranslate)
            {
                case "English":
                    TranslateMethod(final.Text, "en").Wait();
                    break;
                case "German":
                  TranslateMethod(final.Text, "de").Wait();
                    break;
                case "Italian":
                   TranslateMethod(final.Text, "it").Wait();
                    break;
                case "Greek":
                    TranslateMethod(final.Text, "el").Wait();
                    break;
            }
            UserDialogs.Instance.HideLoading ();
            translation.Text=translatedphrase;
        }

       

        private async  Task<string> TranslateMethod(string finalText, string language)
        {
            try
            {
                object[] body = new object[] { new { Text = finalText } };
                var requestBody = JsonConvert.SerializeObject(body);
                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Post;
                    // Construct the URI and add headers.
                    string route = "/translate?api-version=3.0&to="+language;
                    request.RequestUri = new Uri(translatorendpoint + route);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", translatorkey);

                    // Send the request and get response.
                    HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                    // Read response as a string.
                    string result = await response.Content.ReadAsStringAsync();
                    // Deserialize the response using the classes created earlier.
                    TranslationResult[] deserializedOutput = JsonConvert.DeserializeObject<TranslationResult[]>(result);
                    // Iterate over the deserialized results.
                    string completed="";
                    foreach (TranslationResult o in deserializedOutput)
                    {
                        completed=o.Translations[0].Text;
                    }

                    translatedphrase=completed;
                    return completed;
                }
            }
            catch (Exception e)
            {
                await DisplayAlert("Error", e.ToString (), "OK");
            }

            return null;
        }
    }
}