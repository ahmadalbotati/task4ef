using System.Globalization;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using NationalBank;

using var db = new BankContext();

while (true)
{
    Console.Clear();
    Console.WriteLine("===");
    Console.WriteLine("  National Bank - Management");
    Console.WriteLine("===");
    Console.WriteLine("  1) Add a new Customer");
    Console.WriteLine("  2) Open a new Account for a Customer");
    Console.WriteLine("  3) Update Account Status (Active / Closed)");
    Console.WriteLine("  4) Remove an Account from a Customer");
    Console.WriteLine("  5) List all Customers (with accounts)");
    Console.WriteLine("  0) Exit");
    Console.WriteLine("---");
    Console.Write("  Enter choice: ");
    var choice = Console.ReadLine()?.Trim();

    Console.Clear();
    switch (choice)
    {
        case "1": AddCustomer(); break;
        case "2": OpenAccount(); break;
        case "3": UpdateAccountStatus(); break;
        case "4": RemoveAccount(); break;
        case "5": ListCustomers(); break;
        case "0":
            db.Database.CloseConnection();
            return;
        default:
            Console.WriteLine("Invalid choice. Please enter a number from the menu.");
            break;
    }

    Console.WriteLine();
    Console.WriteLine("Press any key to return to the menu...");
    Console.ReadKey(true);
}

void AddCustomer()
{
    Console.WriteLine("--- Add New Customer ---");
    var fullName = ReadText("Full Name");
    var nationalId = ReadDigits("National ID", 10, 14);
    var dob = ReadDate("Date of Birth : (yyyy-MM-dd)");
    var email = ReadEmail("Email");
    var phone = ReadDigits("Phone", 10, 15);
    var address = ReadText("Address");
    Console.WriteLine("Customer Type:");
    Console.WriteLine("    1) Individual");
    Console.WriteLine("    2) Business");
    var type = ReadInt("  Choice", 1, 2);

    var customer = new Customer
    {
        FullName = fullName,
        NationalId = nationalId,
        DateOfBirth = dob,
        Email = email,
        PhoneNumber = phone,
        Address = address,
        CustomerType = (CustomerType)type
    };
    db.Customers.Add(customer);
    db.SaveChanges();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Customer created successfully. CustomerId = {customer.Id}");
    Console.ResetColor();
}

void OpenAccount()
{
    Console.WriteLine("--- Open New Account ---");
    var accountNumber = ReadText("Account Number");
    Console.WriteLine("Account Type:");
    Console.WriteLine("    1) Savings");
    Console.WriteLine("    2) Current");
    Console.WriteLine("    3) Business");
    var accountType = ReadInt("  Choice", 1, 3);
    var branchCode = ReadText("Branch Code");
    var customerId = ReadInt("Customer Id", 1, int.MaxValue);
    Console.WriteLine("Ownership Role:");
    Console.WriteLine("    1) Primary");
    Console.WriteLine("    2) CoHolder");
    var role = ReadInt("  Choice", 1, 2);

    Console.WriteLine($"Validating branch '{branchCode}' and customer #{customerId}...");
    if (db.Branches.Find(branchCode) == null)
    {
        PrintError($"Branch '{branchCode}' does not exist.");
        return;
    }
    if (db.Customers.Find(customerId) == null)
    {
        PrintError($"Customer #{customerId} does not exist.");
        return;
    }

    var account = db.Accounts.Find(accountNumber);
    if (account == null)
    {
        account = new Account
        {
            AccountNumber = accountNumber,
            AccountType = (AccountType)accountType,
            CurrentBalance = 0,
            OpeningDate = DateTime.Now,
            BranchCode = branchCode
        };
        db.Accounts.Add(account);
    }
    else if (db.CustomerAccounts.Any(ca => ca.AccountNumber == accountNumber && ca.CustomerId == customerId))
    {
        PrintError("This customer is already linked to this account.");
        return;
    }

    db.CustomerAccounts.Add(new CustomerAccount
    {
        CustomerId = customerId,
        AccountNumber = accountNumber,
        OwnershipType = (OwnershipType)role,
        OwnershipStartDate = DateTime.Now,
        AccountStatus = AccountStatus.Active
    });
    db.SaveChanges();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Account '{accountNumber}' created and linked to customer {customerId} as {(OwnershipType)role} owner.");
    Console.ResetColor();
}

void UpdateAccountStatus()
{
    Console.WriteLine("--- Update Account Status ---");
    var accountNumber = ReadText("Account Number");
    var customerId = ReadInt("Customer Id", 1, int.MaxValue);
    var link = db.CustomerAccounts.Find(customerId, accountNumber);
    if (link == null)
    {
        PrintError("No account with this number is linked to this customer.");
        return;
    }
    Console.WriteLine("New Status:");
    Console.WriteLine("    1) Active");
    Console.WriteLine("    2) Closed");
    var status = ReadInt("  Choice", 1, 2);

    link.AccountStatus = (AccountStatus)status;
    db.SaveChanges();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Status updated to {link.AccountStatus}.");
    Console.ResetColor();
}

void RemoveAccount()
{
    Console.WriteLine("--- Remove Account From Customer ---");
    var accountNumber = ReadText("Account Number");
    var customerId = ReadInt("Customer Id", 1, int.MaxValue);
    var link = db.CustomerAccounts.Find(customerId, accountNumber);
    if (link == null)
    {
        PrintError("No account with this number is linked to this customer.");
        return;
    }

    var lastOwner = !db.CustomerAccounts.Any(ca => ca.AccountNumber == accountNumber && ca.CustomerId != customerId);
    db.CustomerAccounts.Remove(link);
    if (lastOwner)
    {
        var account = db.Accounts.Find(accountNumber);
        if (account != null) db.Accounts.Remove(account);
    }
    db.SaveChanges();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Ownership link deleted.");
    if (lastOwner)
        Console.WriteLine($"    That was the last owner - account '{accountNumber}' was also removed.");
    Console.ResetColor();
}

void ListCustomers()
{
    Console.WriteLine("--- All Customers ---");
    var customers = db.Customers
        .AsNoTracking()
        .Include(c => c.CustomerAccounts)
            .ThenInclude(ca => ca.Account)
                .ThenInclude(a => a.Branch)
        .OrderBy(c => c.Id)
        .ToList();

    foreach (var c in customers)
    {
        Console.WriteLine($"  #{c.Id} {c.FullName} ({c.CustomerType})");
        if (c.CustomerAccounts.Count == 0)
        {
            Console.WriteLine("      (no accounts)");
            continue;
        }
        foreach (var ca in c.CustomerAccounts)
        {
            var a = ca.Account;
            Console.WriteLine($"      {a.AccountNumber,-10} {a.AccountType,-9} Balance: {a.CurrentBalance,12:N2}  {ca.OwnershipType,-8} {ca.AccountStatus,-7} @ {a.Branch.Name}");
        }
    }
}

static void PrintError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(message);
    Console.ResetColor();
}

static string ReadText(string label)
{
    while (true)
    {
        Console.Write($"{label} : ");
        var value = Console.ReadLine()?.Trim();
        if (!string.IsNullOrWhiteSpace(value)) return value;
        PrintError("Value cannot be empty. Please try again.");
    }
}

static string ReadDigits(string label, int min, int max)
{
    while (true)
    {
        var value = ReadText(label);
        if (value.All(char.IsDigit) && value.Length >= min && value.Length <= max) return value;
        PrintError($"{label} must be {min} to {max} digits. Please try again.");
    }
}

static string ReadEmail(string label)
{
    while (true)
    {
        var value = ReadText(label);
        if (MailAddress.TryCreate(value, out _)) return value;
        PrintError("Invalid email format. Please try again.");
    }
}

static DateTime ReadDate(string label)
{
    while (true)
    {
        var value = ReadText(label);
        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            && date < DateTime.Today)
            return date;
        PrintError("Invalid date. Use the format yyyy-MM-dd and a past date.");
    }
}

static int ReadInt(string label, int min, int max)
{
    while (true)
    {
        var value = ReadText(label);
        if (int.TryParse(value, out var number) && number >= min && number <= max) return number;
        PrintError("Invalid number. Please try again.");
    }
}