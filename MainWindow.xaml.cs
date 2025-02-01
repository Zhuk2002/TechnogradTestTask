using System.Text;
using System.Windows;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading.Tasks;

namespace TechnogradTestTask;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private string _apiKey;
    
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if(e.Key == Key.Enter)
        {
            LocateButton_Click(sender, e);
        }
    }
    
    private async void LocateButton_Click(object sender, RoutedEventArgs e)
    {
        _apiKey = ApiKeyTextBox.Text;
        if(string.IsNullOrWhiteSpace(_apiKey))
        {
            ApiKeyTextBox.BorderBrush = new SolidColorBrush(Colors.Red);
            ApiKeyTextBox.BorderThickness = new Thickness(2);
            ApiKeyTextBox.ToolTip = "API-key is required";
            CoordinatesTextBlock.Text = "Coordinates: API-key is required!";
            return;
        }
        
        string address = AddressTextBox.Text;
        if(string.IsNullOrWhiteSpace(address))
        {
            AddressTextBox.BorderBrush = new SolidColorBrush(Colors.Red);
            AddressTextBox.BorderThickness = new Thickness(2);
            AddressTextBox.ToolTip = "Address is required";
            CoordinatesTextBlock.Text = "Coordinates: Address is required!";
            return;
        }
        
        string coordinates = await GetResponseAsync(address);
        CoordinatesTextBlock.Text = $"Coordinates: {coordinates}";
        
        ApiKeyTextBox.ClearValue(BorderBrushProperty);
        ApiKeyTextBox.ClearValue(BorderThicknessProperty);
        AddressTextBox.ClearValue(BorderBrushProperty);
        AddressTextBox.ClearValue(BorderThicknessProperty);
    }
    
    /// <summary>
    /// Ассинхронно отправляет GET-запрос в Yandex Geocoding API для получения географических координат для указанного адреса.
    /// </summary>
    /// <param name="address">Адрес для получения координат.</param>
    /// <returns>
    /// Возвращает задачу, представляющую асинхронную операцию. Результат задачи содержит
    /// строку с координатами, если успешно, или сообщение об ошибке, если неудачно.
    /// </returns>

    private async Task<string> GetResponseAsync(string address)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://geocode-maps.yandex.ru/1.x/?apikey={_apiKey}&geocode={address}&format=json";
        HttpResponseMessage response = await client.GetAsync(url);
        
        string jsonResponse = await response.Content.ReadAsStringAsync();
        
        if(response.IsSuccessStatusCode)
        {
            return GetCoordsFromResponse(jsonResponse);
        }
        else
        {
            return GetErrorFromResponse(jsonResponse);
        }
    }
    
    /// <summary>
    /// Достает географические координаты из JSON-ответа Yandex Geocoding API.
    /// </summary>
    /// <param name="jsonResponse">JSON-ответ Yandex Geocoding API</param>
    /// <returns>
    /// Строка с координатами, если успешно, или "Адрес не найден", если неудачно.
    /// </returns>
    private string GetCoordsFromResponse(string jsonResponse)
    {
        using JsonDocument document = JsonDocument.Parse(jsonResponse);
        try
        {
            var posElement = document.RootElement
                .GetProperty("response")
                .GetProperty("GeoObjectCollection")
                .GetProperty("featureMember")[0]
                .GetProperty("GeoObject")
                .GetProperty("Point")
                .GetProperty("pos");
            return posElement.GetString();
        }
        catch
        {
            return "Address not found";
        }
    }
    
    /// <summary>
    /// Достает информацию об ошибке из JSON-ответа Yandex Geocoding API.
    /// </summary>
    /// <param name="jsonResponse">JSON-ответ Yandex Geocoding API</param>
    /// <returns>
    /// Строка с информацией об ошибке, если успешно, или "Неизвестная ошибка", если неудачно.
    /// </returns>
    private string GetErrorFromResponse(string jsonResponse)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(jsonResponse);
            string statusCode = document.RootElement.GetProperty("statusCode").GetInt32().ToString();
            string error = document.RootElement.GetProperty("error").GetString();
            string message = document.RootElement.GetProperty("message").GetString();
            return $"Error {statusCode}: {error} - {message}";
        }
        catch(Exception ex)
        {
            return $"Unknown error - {ex.Message}";
        }
    }
}