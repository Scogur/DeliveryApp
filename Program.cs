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
        Console.WriteLine("Starting");
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.Sources.Clear();

        Generator.GenerateJson(1000);

        builder.Configuration
            .AddXmlFile("config.xml", optional: true, reloadOnChange: true);
        Parser parser = new();
        if (args is { Length: > 0 })
        {
            builder.Configuration.AddCommandLine(args);
            using IHost host = builder.Build();
        }
        string? path = builder.Configuration["key:_path"];
        if (path == null) Console.WriteLine("Path is empty or not found");
        else parser.path = path;
        Order[] orders = parser.Parse();
        Console.WriteLine("Orders found: " + orders.Length);
        string? district = builder.Configuration["key:_district"];
        parser.district = district;
        string? firstDeliveryDateTime = builder.Configuration["key:_firstDeliveryDateTime"];
        parser.firstDeliveryDateTime = firstDeliveryDateTime;
        parser.Validator(args);
        

        Order[] result = parser.Sort(orders);

        if (result.Length > 0)
        {
            Console.WriteLine("Filtered found: " + result.Length);
            Console.WriteLine(JsonSerializer.Serialize(result));
        } else Console.WriteLine("No results found");
    }
}

public class Parser
{
    public string path = "";
    public string? district = "";
    public Guid district_id;
    public string? firstDeliveryDateTime;
    public Order[] Parse()
    {
        Console.WriteLine("Parsing " + path);
        string jsonString = File.ReadAllText(this.path);
        Order[]? orders = JsonSerializer.Deserialize<Order[]>(jsonString);
        return orders ?? throw new InvalidOperationException("Invalid JSON format");
    }

    public bool Validator(string[] args)
    {
        bool allFine = true;
        Console.WriteLine("Found parameters:");
        foreach (var arg in args)
        {
            if (arg.Equals("_path"))
            {
                this.path = args[Array.IndexOf(args, arg) + 1];
            }
            if (arg.Equals("_district"))
            {
                this.district = args[Array.IndexOf(args, arg) + 1];
                Console.WriteLine("_district: " + this.district);
            }
            if (arg.Equals("_firstDeliveryDateTime"))
            {
                this.firstDeliveryDateTime = args[Array.IndexOf(args, arg) + 1];
                Console.WriteLine("_firstDeliveryDateTime: " + this.firstDeliveryDateTime);
            }
        }
        if (this.path.Equals(""))
        {
            Console.WriteLine("Path is empty or not found");
            allFine = false;
        }
        return allFine;
    }

    public static Guid? DistrictGuid(string name)
    {
        // return Districts.DistrictsGuids[name];
        return null;
    }

    public Order[] Sort(Order[] orders)
    {
        if (string.IsNullOrEmpty(this.firstDeliveryDateTime)){
            return SortDistrict(this.district, orders);
        }
        else {
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



public static class Generator
{
    public static void GenerateJson(int lenght)
    {
        string docPath = "./example.json";
        Order[] orders = new Order[lenght];
        for (int i = 0; i < lenght; i++)
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
