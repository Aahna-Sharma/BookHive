using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using WebApplication_Project1_Ecommerce.DataAccess;
using WebApplication_Project1_Ecommerce.DataAccess.Data;
using WebApplication_Project1_Ecommerce.DataAccess.Repository;
using WebApplication_Project1_Ecommerce.DataAccess.Repository.IRepository;
using WebApplication_Project1_Ecommerce.Utility;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("conStr") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
//    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddDefaultTokenProviders().AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddRazorPages();

//builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
//builder.Services.AddScoped<IcoverTypeRepository, coverTypeRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.ConfigureApplicationCookie(Options =>
{
    Options.LoginPath = $"/Identity/Account/Login";
    Options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
    Options.LogoutPath = $"/Identity/Account/Logout";
});

builder.Services.AddAuthentication().AddFacebook(options =>
{
    options.AppId = "1539686324385042";
    options.AppSecret = "cc0de4a65023c52a8a7c240bce287863";
    options.Events.OnRemoteFailure = context =>
    {
        context.Response.Redirect("/Identity/Account/Register");
        context.HandleResponse(); // 🔥 THIS LINE IS CRITICAL
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthentication().AddGoogle(options =>
{
    options.ClientId = "1055187770177-rr5a3jrjudv889hfg6v7g41fqq7ce912.apps.googleusercontent.com";
    options.ClientSecret = "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";
});

builder.Services.AddAuthentication().AddTwitter(options =>
{
    options.ConsumerKey = "9NV6pe9GOdpWoaCjT4dC28umT";
    options.ConsumerSecret = "AfMn49pBVf94czFzvSl3fttENuavke39chBRwgQMufu15qEr7S";
    
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.Configure<StripeSettings>
    (builder.Configuration.GetSection("StripeSettings"));

builder.Services.Configure<EmailSettings>
    (builder.Configuration.GetSection("EmailSettings"));

var app =  builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

StripeConfiguration.ApiKey = builder.Configuration.GetSection("StripeSettings")["Secretkey"];

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
