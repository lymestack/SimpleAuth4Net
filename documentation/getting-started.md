# Getting Started with SimpleAuth for .NET

Note from the Developer: I hope to automate much of this initial set up process in the future, but for now, here are the instructions.

## Prerequisites

The following software packages are required to follow along with this documentation:

- [Microsoft .NET Framework 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [Microsoft SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (any edition) - For this documentation we will be using the free Express Edition in this documentation
- [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) - Used for creating the database.
- [NodeJS](https://www.nodejs.org)

## Step 1: Download the repo

``` terminal
git clone https://repolinkgoeshere/simple-auth
cd simple-auth
```

## Step 2: Update your appsettings.json file

At a minimum, you should replace the `TokenSecret` value in the [App Settings](./app-settings.md) file. A randomly generated 128 byte (or more) secret should be used for this value.

## Step 3: Setup your Database

1. Open SSMS and connect to the `.\SQLEXPRESS` instance.
2. Create a new database or pick an existing database. For this doc, we are creating a new database called `SimpleAuth`.
3. Right click the new database and select the "New Query" option.
4. Copy the contents of the file `WebApi/SimleAuthNet/Data/CreateDb.sql` into the query window and press F5 to run the query.

## Step 4: Run the API & App

1. In a command prompt, change directories to the location of your WebApi and type `dotnet run`. At this point, your API should be running. You can test this by navigating to: `https://localhost:7214/AppConfig`. The contents you see on this page should render a response similar to the one documented on the [Configuration Endpoint] documentation page.
1. To run the Angular version of the app (currently the most refined version in these early stages of development), open a new command prompt window and navigate to the `ng-app` directory and type `npm start`. This may take a moment the first time you run this command to download and install NodeJS dependencies, but should run much faster on subsequent runs. When the Angular app is done compiling, you should be able to load the application at `http://localhost:4200`.
1. Click the "Test Secure Resource" button to demonstrate an attempt to reach a secure endpoint on the API. You should receive a red error message indicating that the resource could not be accessed.

## Step 5: Create your First User

1. On the home page, click the "Login" button and then the "Register" button.
2. Fill out your information and click the Submit button. By default, the first user created in the system will be given "Admin" role access.

## Step 6: Login to the Application

1. Click the "Login" link.
2. Enter the credentials you just created.
3. After logging in, click the "Test Secure Resource" button. You should receive a success message saying that the endpoint worked.

## Step 7: Configure Google SSO (Optional)

Instructions for configuring this can be found on the [Google SSO](./google-sso.md) page.

## Step 8: Create Smtp Pickup Directory

Create a folder called `C:\SmtpPickup` on your local computer. This folder will catch any forgot password emails that go out in a simulated manner.

## Step 9: Run the app

On Windows, you can simply run the `Run.bat` batch file in the root of the project repo or you can run the following commands in two separate terminal windows starting from the repo root directory:

### Command Windows 1 - Run the API

``` console
cd WebApi
dotnet run
```

### Command Windows 2 - Run the Angular App

``` console
cd ng-app
npm install
npm start
```

That's it! You should have a working app up and running. From there you can continue to explore the [Angular App](./angular-app.md).
