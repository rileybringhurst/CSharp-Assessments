using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace SecurityNationalDemo
{
    // Change filepaths, overtime rates, or tax rates here
    static class Constants
    {
        public const string inputFilePath = "D:\\Users\\Riley\\Downloads\\Employees.txt";
        public const string outputFolder = "D:\\Users\\Riley\\Downloads\\";
        public const double otRate = 1.5;
        public const double otThreshold = 80; // hours before otRate takes effect
        public const double doubleOtRate = 1.75;
        public const double doubleOtThreshold = 90; // hours before doubleOtRate takes effect
        public const double fedTaxRate = .15; 
    }
    
    // Track paychecks from each state
    // Be able to return median hours worked, median net pay, and total state taxes
    public class State
    {
        private List<double> hoursWorkedList;
        private List<double> netPayList;
        private double totalTaxes;

        public State()
        {
            this.hoursWorkedList = new List<double>();
            this.netPayList = new List<double>();
            this.totalTaxes = 0;
        }

        public void addPaycheck(double hoursWorked, double netPay, double stateTax)
        {
            this.hoursWorkedList.Add(hoursWorked);
            this.netPayList.Add(netPay);
            this.totalTaxes += stateTax;
            return;
        }

        public string getStateData(string stateName)
        {
            hoursWorkedList.Sort();
            netPayList.Sort();
            double medianHours;
            double medianNetPay;

            if(this.hoursWorkedList.Count % 2 == 0)
            {
                medianHours = (hoursWorkedList[hoursWorkedList.Count / 2] + hoursWorkedList[(hoursWorkedList.Count / 2) - 1])/2;
                medianNetPay = (netPayList[netPayList.Count / 2] + netPayList[(netPayList.Count / 2) - 1]) / 2;
            }
            else
            {
                medianHours = hoursWorkedList[(hoursWorkedList.Count - 1) / 2];
                medianNetPay = netPayList[(netPayList.Count - 1) / 2];
            }

            return stateName+","+medianHours.ToString("0.##")+","+medianNetPay.ToString("0.##")+","+totalTaxes.ToString("0.##");
        }
    }

    // Track paycheck data for each employee
    public class Employee
    {
        public string id;
        public string firstName;
        public string lastName;
        private char payType;
        private double payRate;
        public DateTime startDate;
        private string state;
        private int hoursWorked;
        public double grossPay;
        private double federalTax;
        private double stateTax;
        private double netPay;
        public int yearsWorked;

        public Employee(string id, string firstName, string lastName, char payType, double payRate, DateTime startDate, string state, int hoursWorked, double grossPay, double stateTax, double netPay)
        {
            // Record basic information
            this.id = id;
            this.firstName = firstName;
            this.lastName = lastName;
            this.payType = payType;
            this.payRate = payRate;
            this.startDate = startDate;
            this.state = state;
            this.hoursWorked = hoursWorked;
            this.grossPay = grossPay;
            this.federalTax = grossPay * Constants.fedTaxRate;
            this.stateTax = stateTax;
            this.netPay = netPay;

            // calculate years worked
            int years = DateTime.Now.Year - startDate.Year;
            if (((startDate.Month == DateTime.Now.Month) && (startDate.Day > DateTime.Now.Day)) || (startDate.Month > DateTime.Now.Month)) { years--; }
            this.yearsWorked = years;
        }

        public string getPaycheck()
        {
            return this.id + "," + this.firstName + "," + this.lastName+ "," + this.grossPay.ToString("0.##") + "," + this.federalTax.ToString("0.##") + "," + this.stateTax.ToString("0.##") + "," + this.netPay.ToString("0.##");
        }

        public string getTopEarnerData()
        {
            return this.firstName + "," + this.lastName + "," + this.yearsWorked.ToString() + "," + this.grossPay.ToString("0.##");
        }

    }

    class Program
    {
        
        // Calculate OT pay first, then regular pay
        static double calculateGrossForHourly(double payRate, int hoursWorked)
        {
            double gross = 0;
            if (hoursWorked > Constants.doubleOtThreshold)
            {
                gross += (hoursWorked - Constants.doubleOtThreshold) * Constants.doubleOtRate * payRate;
                hoursWorked -= hoursWorked - Convert.ToInt32(Constants.doubleOtThreshold);
            }

            if (hoursWorked > Constants.otThreshold)
            {
                gross += (hoursWorked - Constants.otThreshold) * Constants.otRate * payRate;
                hoursWorked -= hoursWorked - Convert.ToInt32(Constants.otThreshold);
            }

            gross += hoursWorked * payRate;
            return gross;
        }

        // The prompt requested a function that returns an employee by id "efficiently"
        // I don't think this is necessary in practice, because I can just call employees[id] in main
        static Employee GetEmployeeById(string id, Dictionary<string, Employee> employees) {
            try
            {
                return employees[id];
            }
            catch (Exception e)
            {
                Console.WriteLine("Probably tried GetEmployeeId() on an employee that wasn't in the dictionary. Compiler message- "+e);
                return null;
            }
        }

        static void Main(string[] args)
        {
            // Change state tax rates here
            Dictionary<string, double> taxRates = new Dictionary<string, double>();
            taxRates.Add("UT", .05);
            taxRates.Add("WY", .05);
            taxRates.Add("NV", .05);
            taxRates.Add("CO", .065);
            taxRates.Add("ID", .065);
            taxRates.Add("AZ", .065);
            taxRates.Add("OR", .065);
            taxRates.Add("WA", .07);
            taxRates.Add("NM", .07);
            taxRates.Add("TX", .07);

            // Initialize states dictionary and employees dictionary          
            Dictionary<string, State> states = new Dictionary<string, State>();
            Dictionary<string,Employee> employees = new Dictionary<string, Employee>();

    

            // Import data, calculate each employee's paycheck, and calculate each state's statistics
            // I copied the .csv reading logic from StackOverflow
            string fullText;
            if (File.Exists(Constants.inputFilePath))
            {
                using (StreamReader sr = new StreamReader(Constants.inputFilePath))
                {
                    while (!sr.EndOfStream)
                    {
                        fullText = sr.ReadToEnd().ToString();//read full content
                        string[] rows = fullText.Split('\n');//split file content to get the rows
                        for (int i = 0; i < rows.Count() - 1; i++)
                        {
                            var regex = new Regex("\\\"(.*?)\\\"");
                            var output = regex.Replace(rows[i], m => m.Value.Replace(",", "\\c"));//replace commas inside quotes
                            string[] rowValues = output.Split(',');//split rows with a comma',' to get the column values
                            {
                                {
                                    try
                                    {
                                        // Calculate pay and taxes
                                        double grossPay;
                                        if (rowValues[3] == "S") { grossPay = double.Parse(rowValues[4]) / 26; }
                                        else { grossPay = calculateGrossForHourly(double.Parse(rowValues[4]), int.Parse(rowValues[7].Substring(0, rowValues[7].Length - 1))); }
                                        double stateTax = grossPay * taxRates[rowValues[6]];
                                        double netPay = (grossPay * (1 - Constants.fedTaxRate)) - stateTax;

                                        // Check if state has a record. Add it if it doesn't
                                        if (states.ContainsKey(rowValues[6]) == false) { states.Add(rowValues[6], new State()); }

                                        // Add paycheck to the appropriate state record
                                        states[rowValues[6]].addPaycheck(int.Parse(rowValues[7].Substring(0, rowValues[7].Length - 1)), netPay, stateTax);

                                        // Add the employee to employees list
                                        employees.Add(rowValues[0], new Employee(rowValues[0], rowValues[1], rowValues[2], char.Parse(rowValues[3]), double.Parse(rowValues[4]), DateTime.Parse(rowValues[5]), rowValues[6], int.Parse(rowValues[7].Substring(0, rowValues[7].Length - 1)), grossPay, stateTax, netPay));
                                    }
                                    catch
                                    {
                                        Console.WriteLine("error");
                                    }
                                }
                            }
                        }
                    }
                }
            }// /import

            // Order employees by gross pay, for the pay checks export
            List<Employee> employeesByPay = employees.Values.ToList();
            employeesByPay.Sort((a, b) => { return b.grossPay.CompareTo(a.grossPay); });

            // Export paychecks as Paychecks.csv
            string[] allRows = new string[employees.Count + 1];
            allRows[0] = "Employee ID,First Name,Last Name,Gross Pay,Federal Tax,State Tax,Net Pay";
            int count = 0;
            foreach (var employee in employeesByPay)
            {
                count++;
                allRows[count] = employee.getPaycheck();
            }
            System.IO.File.WriteAllLines(Constants.outputFolder + "Paychecks.csv", allRows);
            // /paychecks

            // Filter for top 15% earners, by gross pay
            // Rounding up to be inclusive
            count = 0;
            int maxRows = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(employees.Count) * .15));
            employeesByPay.RemoveRange(maxRows, employeesByPay.Count - maxRows);
       
            // Sort top 15% earners. Ordered by yearsWorked desc, lastName asc, firstName asc
            employeesByPay.Sort((a, b) =>
            {
                int result = b.yearsWorked.CompareTo(a.yearsWorked);
                if (result == 0) { result = a.lastName.CompareTo(b.lastName); }
                if (result == 0) { result = a.firstName.CompareTo(b.firstName); }
                return result;
            });

            // Export top 15% earners data to TopEarners.csv
            allRows = new string[employeesByPay.Count + 1];
            allRows[0] = "First Name,Last Name,Years Worked,Gross Pay";
            count = 0;
            foreach(var employee in employeesByPay)
            {
                count++;
                allRows[count] = employee.getTopEarnerData();
            }
            System.IO.File.WriteAllLines(Constants.outputFolder + "TopEarners.csv", allRows);
            // /topEarners

            // Sort states alphabetically by abbreviation
            List<string> stateKeys = states.Keys.ToList();
            stateKeys.Sort();

            // Export state data to States.csv
            allRows = new string[stateKeys.Count + 1];
            allRows[0] = "State,Median Time Worked,Median Net Pay,State Taxes";
            count = 0;
            foreach(var stateKey in states.Keys.ToList())
            {
                count++;
                allRows[count] = states[stateKey].getStateData(stateKey);
            }
            System.IO.File.WriteAllLines(Constants.outputFolder + "States.csv", allRows);

            return;
        }
    }
}
