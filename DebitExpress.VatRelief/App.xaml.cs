using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DebitExpress.VatRelief.Models;
using DebitExpress.VatRelief.Utils;

namespace DebitExpress.VatRelief;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        const string fileName = @"%USERPROFILE%\Desktop\vat-relief-template.xlsx";

        var reader = new VatTemplateReader();
        var result = await reader.ReadAsync(Environment.ExpandEnvironmentVariables(fileName));
        if (result.IsFaulted)
        {
            MessageBox.Show(result.ToString());
            Environment.Exit(1);
        }

        const string loc = @"%USERPROFILE%\Desktop";
        var path = Environment.ExpandEnvironmentVariables(loc);
        ExcelData data = result;

        var generator = new DatFileGenerator();
        await generator.GenerateAsync(data, path);

        var reconWriter = new ExcelReconWriter();
        reconWriter.WriteReconciliationReport(data, path);
    }
}