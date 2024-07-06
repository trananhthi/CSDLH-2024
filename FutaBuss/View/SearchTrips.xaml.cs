﻿using FutaBuss.DataAccess;
using FutaBuss.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FutaBuss.View
{
    /// <summary>
    /// Interaction logic for SearchTrips.xaml
    /// </summary>
    public partial class SearchTrips : Page
    {
        private MongoDBConnection _mongoDBConnection;
        private RedisConnection _redisConnection;
        private PostgreSQLConnection _postgreSQLConnection;
        private MongoClient client;
        private IMongoDatabase database;
        private IMongoCollection<Trip> tripsCollection;
        private Dictionary<string, string> _provinceDictionary;
        private List<BsonDocument> _originalTrips;
        private List<BsonDocument> _originalRoundTrips;

        public SearchTrips()
        {
            InitializeComponent();
            InitializeDatabaseConnections();

            tripsCollection = _mongoDBConnection.GetCollection<Trip>("trips");

            CacheProvince();
            LoadProvinces();
        }

        private void OneWayRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (ReturnDatePanel != null)
            {
                Grid inputTripInformationGrid = FindName("inputTripInformationGrid") as Grid;
                inputTripInformationGrid.ColumnDefinitions.Clear();
                inputTripInformationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                inputTripInformationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                inputTripInformationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                inputTripInformationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                inputTripInformationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                ReturnDatePanel.Visibility = Visibility.Collapsed;
            }
        }

        private void RoundTripRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (ReturnDatePanel != null)
            {
                Grid inputTripInformationGrid = FindName("inputTripInformationGrid") as Grid;
                inputTripInformationGrid.ColumnDefinitions.Clear();
                inputTripInformationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                inputTripInformationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                inputTripInformationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                inputTripInformationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                inputTripInformationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                inputTripInformationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                ReturnDatePanel.Visibility = Visibility.Visible;
            }
        }
        private void SwapButton_Click(object sender, RoutedEventArgs e)
        {
            // Lưu trữ tạm thời giá trị hiện tại của các ComboBox
            var tempDeparture = DepartureComboBox.SelectedValue;
            var tempDestination = DestinationComboBox.SelectedValue;

            // Hoán đổi giá trị
            DepartureComboBox.SelectedValue = tempDestination;
            DestinationComboBox.SelectedValue = tempDeparture;
        }

        private void CacheProvince()
        {
            var listProvince = new List<Province>();
            listProvince = _postgreSQLConnection.GetProvinces();
            foreach (var province in listProvince)
            {
                var name = _redisConnection.GetString($"province:{province.Code}:name");
                if (name == null)
                {
                    _redisConnection.SetString($"province:{province.Code}:name", province.Name);
                }
            }
        }

        private void InitializeDatabaseConnections()
        {
            try
            {
                // Kết nối MongoDB
                //_mongoDBConnection = new MongoDBConnection("mongodb+srv://thuannt:J396QWpWuiGDZhOs@thuannt.yzjzr9s.mongodb.net/?appName=ThuanNT", "futabus");

                //_redisConnection = new RedisConnection("redis-18667.c8.us-east-1-2.ec2.cloud.redislabs.com:18667", "default", "dVZCrABvG85l0L9JQI9izqn2SDvvTx82");

                //_postgreSQLConnection = new PostgreSQLConnection("Host=dpg-cq12053v2p9s73cjijm0-a.singapore-postgres.render.com;Username=root;Password=vTwWs92lObTZrhI9IFcJGXJxZCdzeBas;Database=mds_postpresql");
                //_postgreSQLConnection.OpenConnection();


                _mongoDBConnection = MongoDBConnection.Instance;
                _redisConnection = RedisConnection.Instance;
                _postgreSQLConnection = PostgreSQLConnection.Instance;

            }
            catch (ApplicationException ex)
            {
                MessageBox.Show(ex.Message, "Database Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProvinces()
        {
            // Đây là danh sách mẫu, bạn có thể lấy từ Redis hoặc nguồn khác
            var provinces = _redisConnection.GetAllProvinces();
            _provinceDictionary = provinces.ToDictionary(p => p.Code, p => p.Name);
            // Gán danh sách provinces cho các ComboBox
            DepartureComboBox.ItemsSource = provinces;
            DestinationComboBox.ItemsSource = provinces;
        }

        private async void SearchTripsButton_Click(object sender, RoutedEventArgs e)
        {
            string departure = DepartureComboBox.SelectedValue as string;
            string destination = DestinationComboBox.SelectedValue as string;
            DateTime? departureDate = DepartureDatePicker.SelectedDate;
            DateTime? returnDate = ReturnDatePicker.SelectedDate;


            if (departureDate == null)
            {
                MessageBox.Show("Vui lòng chọn ngày khởi hành.");
                return;
            }

            string departureDateString = departureDate.Value.ToString("yyyy-MM-dd");
            string returnDateString = returnDate.Value.ToString("yyyy-MM-dd");
            int ticketCount = int.Parse(((ComboBoxItem)TicketCountComboBox.SelectedItem).Content.ToString());

            try
            {
                List<BsonDocument> trips = await _mongoDBConnection.SearchTripsAsync(departure, destination, departureDateString, ticketCount);
                _originalTrips = trips;
                if (RoundTrip.IsChecked == true)
                {
                    List<BsonDocument> roundTrips = await _mongoDBConnection.SearchRoundTripsAsync(destination, departure, returnDateString, ticketCount);
                    _originalRoundTrips = roundTrips;
                }
                DisplayTrips(trips);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tìm kiếm chuyến: {ex.Message}");
            }
        }

        private void SelectTrip(BsonDocument trip)
        {
            //string tripId = trip["id"].ToString();
            //Booking bookingWindow = new Booking(tripId);
            //bookingWindow.Show();


            //Chuyển như code bên dưới
            //MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;


            //if (mainWindow != null && mainWindow.MainFrame != null)
            //{
            //    mainWindow.MainFrame.Navigate(new FutaBuss.View.Book());
            //}
        }

        private int CountEmptySeats(BsonDocument trip)
        {
            int emptySeatCount = 0;

            var floors = trip["seat_config"]["floors"].AsBsonArray;
            foreach (var floor in floors)
            {
                var seats = floor["seats"].AsBsonArray;
                foreach (var seat in seats)
                {
                    if (seat["status"].AsString == "empty")
                    {
                        emptySeatCount++;
                    }
                }
            }

            return emptySeatCount;
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            // Lấy tất cả các điều kiện lọc từ các CheckBox
            bool earlyMorning = EarlyMorningCheckBox.IsChecked ?? false;
            bool morning = MorningCheckBox.IsChecked ?? false;
            bool afternoon = AfternoonCheckBox.IsChecked ?? false;
            bool evening = EveningCheckBox.IsChecked ?? false;

            if (earlyMorning == false && morning == false && afternoon == false && evening == false)
            {
                earlyMorning = true;
                morning = true;
                afternoon = true;
                evening = true;
            }

            bool seatedBus = SeatedBusCheckBox.IsChecked ?? false;
            bool sleeperBus = SleeperBusCheckBox.IsChecked ?? false;
            bool limousine = LimousineCheckBox.IsChecked ?? false;

            if (seatedBus == false && sleeperBus == false && limousine == false)
            {
                seatedBus = true;
                sleeperBus = true;
                limousine = true;
            }

            bool middleRow = MiddleRowCheckBox.IsChecked ?? false;
            bool frontRow = FrontRowCheckBox.IsChecked ?? false;
            bool lastRow = LastRowCheckBox.IsChecked ?? false;

            if (middleRow == false && frontRow == false && lastRow == false)
            {
                middleRow = true;
                frontRow = true;
                lastRow = true;
            }

            bool upperFloor = UpperFloorCheckBox.IsChecked ?? false;
            bool lowerFloor = LowerFloorCheckBox.IsChecked ?? false;

            if (upperFloor == false && lowerFloor == false)
            {
                upperFloor = true;
                lowerFloor = true;
            }

            Debug.WriteLine($"Early Morning: {earlyMorning}");
            Debug.WriteLine($"Morning: {morning}");
            Debug.WriteLine($"Afternoon: {afternoon}");
            Debug.WriteLine($"Evening: {evening}");
            Debug.WriteLine($"Seated Bus: {seatedBus}");
            Debug.WriteLine($"Sleeper Bus: {sleeperBus}");
            Debug.WriteLine($"Limousine: {limousine}");
            Debug.WriteLine($"Middle Row: {middleRow}");
            Debug.WriteLine($"Front Row: {frontRow}");
            Debug.WriteLine($"Last Row: {lastRow}");
            Debug.WriteLine($"Upper Floor: {upperFloor}");
            Debug.WriteLine($"Lower Floor: {lowerFloor}");

            Debug.WriteLine("ApplyFilters called");

            // Khởi tạo FilteredTrips là ObservableCollection<BsonDocument>
            List<BsonDocument> FilteredTrips = new List<BsonDocument>();

            // Lặp qua từng chuyến xe trong _originalTrips
            foreach (var trip in _originalTrips)
            {
                // Parse departure time and date
                DateTime departureDate = DateTime.Parse(trip["departure_date"].ToString());
                TimeSpan departureTime = TimeSpan.Parse(trip["departure_time"].ToString());
                DateTime departureDateTime = departureDate.Add(departureTime);

                string tripType = trip["trip_type"].ToString();

                // Time condition
                bool timeCondition = (earlyMorning && departureDateTime.Hour >= 0 && departureDateTime.Hour < 6) ||
                                     (morning && departureDateTime.Hour >= 6 && departureDateTime.Hour < 12) ||
                                     (afternoon && departureDateTime.Hour >= 12 && departureDateTime.Hour < 18) ||
                                     (evening && departureDateTime.Hour >= 18 && departureDateTime.Hour < 24);
                Debug.WriteLine($"Time Condition for trip {trip["id"]}: {timeCondition}");

                // Type condition
                bool typeCondition = (seatedBus && tripType == "seated_bus") ||
                                     (sleeperBus && tripType == "sleeper_bus") ||
                                     (limousine && tripType == "limousine");
                Debug.WriteLine($"Type Condition for trip {trip["id"]}: {typeCondition}");

                // Row condition
                bool rowCondition = false;
                var floorsArray = trip["seat_config"]["floors"].AsBsonArray;
                foreach (var floor in floorsArray)
                {
                    var seatsArray = floor["seats"].AsBsonArray;
                    if ((middleRow && seatsArray.Any(seat => seat["alias"].ToString().Contains("middle"))) ||
                        (frontRow && seatsArray.Any(seat => seat["alias"].ToString().Contains("front"))) ||
                        (lastRow && seatsArray.Any(seat => seat["alias"].ToString().Contains("last"))))
                    {
                        rowCondition = true;
                        break;
                    }
                }
                Debug.WriteLine($"Row Condition for trip {trip["id"]}: {rowCondition}");

                // Floor condition
                bool floorCondition = (upperFloor && floorsArray.Any(floor => floor["ordinal"].ToInt32() == 1)) ||
                                      (lowerFloor && floorsArray.Any(floor => floor["ordinal"].ToInt32() == 0));
                Debug.WriteLine($"Floor Condition for trip {trip["id"]}: {floorCondition}");

                // Kiểm tra tất cả các điều kiện
                if (timeCondition && typeCondition && rowCondition && floorCondition)
                {
                    // Nếu tất cả điều kiện đều đúng, thêm chuyến xe vào FilteredTrips
                    FilteredTrips.Add(trip);
                }
            }

            Debug.WriteLine($"Filtered Trips: {FilteredTrips.Count}");

            // Hiển thị các chuyến xe đã lọc
            DisplayTrips(FilteredTrips);
        }

        private void DisplayTrips(List<BsonDocument> trips)
        {
            StackPanel resultPanel = FindName("ResultPanel") as StackPanel;
            resultPanel.Children.Clear();
            if (trips.Count > 0)
            {
                string departureProvinceCode = trips[0]["departure_province_code"].ToString();
                string destinationProvinceCode = trips[0]["destination_province_code"].ToString();

                _provinceDictionary.TryGetValue(departureProvinceCode, out string departureProvinceName);
                _provinceDictionary.TryGetValue(destinationProvinceCode, out string destinationProvinceName);

                string tripInfo = $"{(departureProvinceName ?? departureProvinceCode)} - {(destinationProvinceName ?? destinationProvinceCode)} ({trips.Count} chuyến)";

                TextBlock tripInfoTextBlock = new TextBlock
                {
                    Text = tripInfo,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                resultPanel.Children.Add(tripInfoTextBlock);
            }

            foreach (var trip in trips)
            {
                Border tripBorder = new Border
                {
                    BorderBrush = System.Windows.Media.Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                StackPanel tripStack = new StackPanel();

                Grid tripGrid = new Grid();
                tripGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                tripGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                tripGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                tripGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                tripGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                tripGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Thời gian khởi hành
                TextBlock departureTime = new TextBlock
                {
                    Text = trip["departure_time"].ToString(),
                    Margin = new Thickness(0, 10, 143, 16),
                    Width = 150,
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetRow(departureTime, 0);
                Grid.SetColumn(departureTime, 0);
                Grid.SetRowSpan(departureTime, 2);
                tripGrid.Children.Add(departureTime);

                // Thời gian di chuyển
                string travelTime = "---------->";
                TextBlock travelTimeBlock = new TextBlock
                {
                    Text = travelTime,
                    Margin = new Thickness(161, 10, 22, 0),
                    Width = 60,
                    VerticalAlignment = VerticalAlignment.Center
                };
                //Grid.SetRow(travelTimeBlock, 0);
                //Grid.SetColumn(travelTimeBlock, 1);
                tripGrid.Children.Add(travelTimeBlock);

                // Thời gian đến
                TextBlock arrivalTime = new TextBlock
                {
                    Text = trip["expected_arrival_time"].ToString(),
                    Margin = new Thickness(0, 10, 142, 16),
                    Width = 150,
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetRow(arrivalTime, 0);
                Grid.SetColumn(arrivalTime, 2);
                Grid.SetRowSpan(arrivalTime, 2);
                tripGrid.Children.Add(arrivalTime);

                // Loại xe
                string tripTypeCode = trip["trip_type"].ToString();
                Debug.WriteLine(tripTypeCode);
                string tripTypeText = tripTypeCode switch
                {
                    "seated_bus" => "Ghế",
                    "sleeper_bus" => "Giường",
                    "limousine" => "Limousine",
                    _ => tripTypeCode
                };
                TextBlock tripType = new TextBlock
                {
                    Text = tripTypeText,
                    Margin = new Thickness(139, 10, 196, 1),
                    Width = 100,
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetRow(tripType, 0);
                Grid.SetColumn(tripType, 2);
                Grid.SetColumnSpan(tripType, 2);
                tripGrid.Children.Add(tripType);

                // Số giường
                int totalEmptySeats = CountEmptySeats(trip);
                string totalEmptySeatsText = tripTypeCode switch
                {
                    "seated_bus" => $"Ghế trống: {totalEmptySeats}",
                    "sleeper_bus" => $"Giường trống: {totalEmptySeats}",
                    "limousine" => $"Giường trống: {totalEmptySeats}",
                    _ => tripTypeCode
                };
                TextBlock seatCount = new TextBlock
                {
                    Text = totalEmptySeatsText,
                    Margin = new Thickness(242, 10, -1, 16),
                    Width = 150,
                    TextAlignment = TextAlignment.Right
                };
                Grid.SetRowSpan(seatCount, 2);
                Grid.SetColumn(seatCount, 2);
                Grid.SetColumnSpan(seatCount, 2);
                tripGrid.Children.Add(seatCount);

                // Nơi đi
                string departureProvinceCode = trip["departure_province_code"].ToString();
                _provinceDictionary.TryGetValue(departureProvinceCode, out string departureProvinceName);
                TextBlock departureProvince = new TextBlock
                {
                    Text = departureProvinceName ?? departureProvinceCode,
                    Margin = new Thickness(0, 10, 93, 0),
                    Width = 150,
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetRow(departureProvince, 1);
                Grid.SetColumn(departureProvince, 0);
                tripGrid.Children.Add(departureProvince);

                // Nơi đến
                string destinationProvinceCode = trip["destination_province_code"].ToString();
                _provinceDictionary.TryGetValue(destinationProvinceCode, out string destinationProvinceName);
                TextBlock destinationProvince = new TextBlock
                {
                    Text = destinationProvinceName ?? destinationProvinceCode,
                    Margin = new Thickness(0, 10, 92, 0),
                    Width = 150,
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetRow(destinationProvince, 1);
                Grid.SetColumn(destinationProvince, 2);
                tripGrid.Children.Add(destinationProvince);

                // Giá vé
                TextBlock price = new TextBlock
                {
                    Text = trip["price"].ToString() + " VND",
                    Margin = new Thickness(93, 10, 0, 0),
                    Width = 150,
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetRow(price, 1);
                Grid.SetColumn(price, 3);
                tripGrid.Children.Add(price);

                Button selectButton = new Button
                {
                    Content = "Chọn chuyến",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFA5400")),
                    Foreground = Brushes.White,
                    Padding = new Thickness(5, 2, 5, 2)
                };
                selectButton.Click += (sender, e) => SelectTrip(trip);

                Border buttonBorder = new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(10),
                    Width = 150,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Child = selectButton
                };

                tripStack.Children.Add(tripGrid);
                tripStack.Children.Add(buttonBorder);

                tripBorder.Child = tripStack;
                resultPanel.Children.Add(tripBorder);
            }
        }
    }
}