using Microsoft.AspNetCore.Identity;

namespace CarRentalManagement.Data;

/// <summary>
/// Provides methods to seed the database with default Identity
/// roles and an administrator account.
///
/// Unlike HasData(), this approach uses ASP.NET Core Identity
/// managers to create users, roles, and their relationships.
///
/// The seeding process is idempotent, meaning it can be executed
/// multiple times without creating duplicate records.
/// </summary>
public static class IdentitySeeder
{
    /// <summary>
    /// Creates default roles and an administrator account
    /// if they do not already exist in the database.
    /// </summary>
    /// <param name="services">
    /// The service provider used to retrieve Identity services
    /// through Dependency Injection (DI).
    /// </param>
    /// <param name="adminPassword">
    /// The initial password for the administrator account.
    /// This should be supplied through secure configuration.
    /// </param>
    public static async Task SeedAsync(
        IServiceProvider services,
        string adminPassword)
    {
        // Retrieve RoleManager from the Dependency Injection container.
        // RoleManager provides methods to manage Identity roles.
        var roleManager = services
            .GetRequiredService<RoleManager<IdentityRole>>();

        // Retrieve UserManager from the Dependency Injection container.
        // UserManager handles user creation, password hashing,
        // user validation, and role assignments.
        var userManager = services
            .GetRequiredService<UserManager<ApplicationUser>>();

        // ---------------------------------------------------------
        // STEP 1: Create default roles
        // ---------------------------------------------------------

        // Define the roles required by the application.
        string[] roles = ["Administrator", "User"];

        // Loop through each role and create it if it does not exist.
        foreach (var role in roles)
        {
            // Check whether the role already exists in the database.
            if (!await roleManager.RoleExistsAsync(role))
            {
                // Create the role using RoleManager.
                // Identity automatically generates the role ID
                // and normalizes the role name.
                var result = await roleManager.CreateAsync(
                    new IdentityRole(role));

                // Throw an exception if role creation fails.
                EnsureSuccess(result);
            }
        }

        // ---------------------------------------------------------
        // STEP 2: Create the default administrator account
        // ---------------------------------------------------------

        // Define the administrator's email address.
        const string adminEmail = "admin@localhost.com";

        // Search for an existing user with this email address.
        var admin = await userManager.FindByEmailAsync(adminEmail);

        // Only create the administrator if the account
        // does not already exist in the database.
        if (admin == null)
        {
            // Create a new ApplicationUser object.
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",

                // Mark the email as confirmed so the administrator
                // can sign in without email verification.
                EmailConfirmed = true
            };

            // Create the administrator account.
            // UserManager automatically:
            // - Validates the password against Identity rules.
            // - Generates a salted password hash.
            // - Normalizes the username and email.
            // - Generates the user ID and security stamp.
            // - Saves the user to the database.
            var result = await userManager.CreateAsync(
                admin, adminPassword);

            // Throw an exception if user creation fails.
            EnsureSuccess(result);
        }

        // ---------------------------------------------------------
        // STEP 3: Assign the administrator role
        // ---------------------------------------------------------

        // Check whether the administrator already belongs
        // to the Administrator role.
        if (!await userManager.IsInRoleAsync(
            admin, "Administrator"))
        {
            // Assign the Administrator role to the user.
            // Identity automatically creates the relationship
            // in the AspNetUserRoles database table.
            var result = await userManager.AddToRoleAsync(
                admin, "Administrator");

            // Throw an exception if role assignment fails.
            EnsureSuccess(result);
        }
    }

    /// <summary>
    /// Checks whether an ASP.NET Core Identity operation
    /// completed successfully.
    /// </summary>
    /// <param name="result">
    /// The IdentityResult returned by an Identity operation.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Identity operation fails.
    /// </exception>
    private static void EnsureSuccess(IdentityResult result)
    {
        // IdentityResult contains a Succeeded property
        // indicating whether the operation was successful.
        if (!result.Succeeded)
        {
            // Collect all error descriptions into one message
            // to make troubleshooting easier.
            throw new InvalidOperationException(
                string.Join("; ",
                    result.Errors.Select(e => e.Description)));
        }
    }
}