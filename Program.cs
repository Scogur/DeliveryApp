namespace DeliveryApp;

using System;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

class Program
{
    static void Main(string[] args)
    {
        Logger logger = new();
        logger.Log("============Starting new log============");
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.Sources.Clear();

        // Generate JSON for testing
        //Generator.GenerateJson(100000);


        builder.Configuration
            .AddXmlFile("config.xml", optional: true, reloadOnChange: true);
        Parser parser = new();
        parser.logger = logger;
        if (args is { Length: > 0 })
        {
            builder.Configuration.AddCommandLine(args);
            using IHost host = builder.Build();
        }
        string? path = builder.Configuration["key:_path"];
        if (path == null) logger.Log("Path is empty or not found");
        else parser.path = path;
        Order[] orders = parser.Parse();
        logger.Log("Orders found: " + orders.Length);
        string? district = builder.Configuration["key:_district"];
        parser.district = district;
        string? firstDeliveryDateTime = builder.Configuration["key:_firstDeliveryDateTime"];
        parser.firstDeliveryDateTime = firstDeliveryDateTime;

        string? logFile = builder.Configuration["key:_deliveryLog"];
        if (string.IsNullOrWhiteSpace(logFile))
        {
            logFile = "./log.txt";
        }
        logger.logFile = logFile;
        logger.Log("Logs are saved to " + logFile);

        string? outFile = builder.Configuration["key:_deliveryOrder"];
        if (string.IsNullOrWhiteSpace(outFile))
        {
            outFile = "./out.json";
        }
        parser.Validator(args);


        Order[] result = parser.Sort(orders);

        if (result.Length > 0)
        {
            logger.Log("Filtered found: " + result.Length);
            logger.Log("Saving result to " + outFile);
            SaveToFile(outFile, result);
        }
        else logger.Log("No results found");
    }

    static void SaveToFile(string filename, Object content)
    {
        using StreamWriter file = File.CreateText(filename);
        file.WriteLine(JsonSerializer.Serialize(content));
    }
}

public class Logger
{
    public Queue<string> log = new();
    public string? logFile = null;

    public void Log(string message)
    {
        message = "[" + DateTime.Now.ToString() + "]" + message;
        log.Enqueue(message);
        Console.WriteLine(message);
        SaveToFile();
    }
    private void SaveToFile()
    {
        if (!string.IsNullOrWhiteSpace(logFile))
        {
            using StreamWriter file = File.AppendText(logFile);
            while (log.Count>0){
                file.WriteLine(log.Dequeue());
            }
        }
    }
}

public class Parser
{
    public Logger logger;
    public string path = "";
    public string? district = "";
    public Guid district_id;
    public string? firstDeliveryDateTime;
    public Order[] Parse()
    {
        logger.Log("Parsing " + path);
        string jsonString = File.ReadAllText(this.path);
        Order[]? orders = JsonSerializer.Deserialize<Order[]>(jsonString);
        return orders ?? throw new InvalidOperationException("Invalid JSON format");
    }

    public bool Validator(string[] args)
    {
        bool allFine = true;
        logger.Log("Found cli parameters:");
        foreach (var arg in args)
        {
            if (arg.Equals("_path"))
            {
                this.path = args[Array.IndexOf(args, arg) + 1];
            }
            if (arg.Equals("_district"))
            {
                this.district = args[Array.IndexOf(args, arg) + 1];
                logger.Log("_district: " + this.district);
            }
            if (arg.Equals("_firstDeliveryDateTime"))
            {
                this.firstDeliveryDateTime = args[Array.IndexOf(args, arg) + 1];
                logger.Log("_firstDeliveryDateTime: " + this.firstDeliveryDateTime);
            }
        }
        if (this.path.Equals(""))
        {
            logger.Log("Path is empty or not found");
            allFine = false;
        }
        return allFine;
    }

    public Order[] Sort(Order[] orders)
    {
        if (string.IsNullOrWhiteSpace(this.firstDeliveryDateTime))
        {
            return SortDistrict(this.district, orders);
        }
        else
        {
            return SortDistrict(this.district, SortDate(DateTime.Parse(this.firstDeliveryDateTime), orders));
        }
    }

    public static Order[] SortDistrict(string? district, Order[] orders)
    {
        if (district == null) return orders;
        List<Order> sortedOrders = [];
        foreach (Order order in orders)
        {
            if (order.district == district)
                sortedOrders.Add(order);
        }
        return [.. sortedOrders];
    }
    public static Order[] SortDate(DateTime dateTime, Order[] orders)
    {
        List<Order> sortedOrders = [];
        foreach (Order order in orders)
        {
            DateTime orderDate = DateTime.Parse(order.Date);
            if ((dateTime <= orderDate) && (orderDate <= dateTime.AddMinutes(30)))
                sortedOrders.Add(order);
        }
        return [.. sortedOrders];
    }
}

public class Order
{
    public Guid id { get; set; }
    public float weight { get; set; }
    public required string district { get; set; }
    public required string Date { get; set; }
}

public class Districts
{
    public static List<string> DistrictsGuids = new()
        {
        {"d1"},
        {"d2"},
        {"d3"},
        };
}





//==================================
//Generator class purely is for testing purposes
//It contains GenerateJson method that generates example file with specific amount of items
//==================================
public static class Generator
{
    public static void GenerateJson(int length)
    {
        string docPath = "./example.json";
        Order[] orders = new Order[length];
        for (int i = 0; i < length; i++)
        {
            orders[i] = new Order()
            {
                id = Guid.NewGuid(),
                weight = (float)new Random().Next(1, 10),
                district = Districts.DistrictsGuids.ElementAt(
                    new Random().Next(0, Districts.DistrictsGuids.Count)
                    ),
                Date = DateTime.Now.AddMinutes(i).ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss"),
            };
        }
        using StreamWriter file = File.CreateText(docPath);
        file.WriteLine(JsonSerializer.Serialize(orders));
    }

}
