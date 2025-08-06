# NckExchange

NckExchange is a web application built with Umbraco CMS (.NET 8) designed for flexible content management and modern web experiences.

## Features
- Built on Umbraco CMS (v13+)
- Modular content structure (Pages, Partials, Macros)
- Custom navigation and site settings
- uSync for configuration and content migration
- Responsive design with Bootstrap

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Lite (default) or SQL Server (optional)
- Node.js (for frontend asset builds, if needed)

### Installation
1. **Clone the repository:**
   ```sh
   git clone https://github.com/jonnymuir/NckExchange.git
   cd NckExchange
   ```
2. **Restore NuGet packages:**
   ```sh
   dotnet restore
   ```
3. **Run the application:**
   ```sh
   dotnet run --project NckExchange/NckExchange.csproj
   ```
4. **Access the site:**
   Open [http://localhost:5000](http://localhost:5000) (or the port shown in your terminal)

### Configuration
- Edit `appsettings.json` for environment-specific settings.
- Umbraco database is stored in `umbraco/Data/Umbraco.sqlite.db` by default.
- uSync configuration is in `uSync/`.

### Handling Secrets in Development
For sensitive settings like `unattendedUserPassword`, use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) in development:

1. Initialize user secrets in your project directory:
   ```sh
   dotnet user-secrets init
   ```
2. Set the secret (replace with your actual password):
   ```sh
   dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserPassword" "yourStrongPassword"
   ```
3. Remove the password from `appsettings.Development.json` if present.

User secrets are stored outside your project and are not committed to git, keeping your credentials safe.

### Development
- Views are in `NckExchange/Views/`
- Static assets in `NckExchange/wwwroot/`
- Models in `NckExchange/Models/`

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## Publish

```
dotnet publish -c Release --no-self-contained
```

## Test
```
npx playwright test
```
## License
[MIT](LICENSE)

---

> Built with ❤️ using Umbraco CMS and .NET 8.  
