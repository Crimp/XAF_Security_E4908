﻿using BusinessObjectsLibrary.BusinessObjects;
using DevExpress.ExpressApp.DC.Xpo;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using DevExpress.ExpressApp.Xpo;
using DevExpress.ExpressApp.Core;
using DevExtreme.OData.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(mvcOptions => {
        mvcOptions.EnableEndpointRouting = false;
    })
    .AddOData(opt => opt
        .Count()
        .Filter()
        .Expand()
        .Select()
        .OrderBy()
        .SetMaxTop(null)
        .AddRouteComponents(GetEdmModel())
    );

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services
    .AddSingleton<ITypesInfo>((serviceProvider) => {
        TypesInfo typesInfo = new TypesInfo();
        typesInfo.GetOrAddEntityStore(ti => new XpoTypeInfoSource(ti));
        typesInfo.RegisterEntity(typeof(Employee));
        typesInfo.RegisterEntity(typeof(PermissionPolicyUser));
        typesInfo.RegisterEntity(typeof(PermissionPolicyRole));
        return typesInfo;
    })
    .AddScoped<IObjectSpaceProviderFactory, ObjectSpaceProviderFactory>()
    .AddSingleton<IXpoDataStoreProvider>((serviceProvider) => {
        var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("ConnectionString");
        return XPObjectSpaceProvider.GetDataStoreProvider(connectionString, null, true);
    });
builder.Services.AddXafAspNetCoreSecurity(builder.Configuration, options => {
    options.RoleType = typeof(PermissionPolicyRole);
    options.UserType = typeof(PermissionPolicyUser);
    options.Events.OnSecurityStrategyCreated = strategy => ((SecurityStrategy)strategy).RegisterXPOAdapterProviders();
    options.SupportNavigationPermissionsForTypes = false;
}).AddAuthenticationStandard();
var app = builder.Build();

if(app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
}
else {
    app.UseHsts();
}
app.UseODataQueryRequest();
app.UseODataBatching();

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<UnauthorizedRedirectMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCookiePolicy();
app.UseEndpoints(endpoints => {
    endpoints.MapControllers();
});
app.UseDemoData();

app.Run();

IEdmModel GetEdmModel() {
    ODataModelBuilder builder = new ODataConventionModelBuilder();
    EntitySetConfiguration<Employee> employees = builder.EntitySet<Employee>("Employees");
    EntitySetConfiguration<Department> departments = builder.EntitySet<Department>("Departments");
    EntitySetConfiguration<Party> parties = builder.EntitySet<Party>("Parties");
    EntitySetConfiguration<ObjectPermission> objectPermissions = builder.EntitySet<ObjectPermission>("ObjectPermissions");
    EntitySetConfiguration<MemberPermission> memberPermissions = builder.EntitySet<MemberPermission>("MemberPermissions");
    EntitySetConfiguration<TypePermission> typePermissions = builder.EntitySet<TypePermission>("TypePermissions");

    employees.EntityType.HasKey(t => t.Oid);
    departments.EntityType.HasKey(t => t.Oid);
    parties.EntityType.HasKey(t => t.Oid);

    ActionConfiguration login = builder.Action("Login");
    login.Parameter<string>("userName");
    login.Parameter<string>("password");

    builder.Action("Logout");

    ActionConfiguration getPermissions = builder.Action("GetPermissions");
    getPermissions.Parameter<string>("typeName");
    getPermissions.CollectionParameter<string>("keys");

    ActionConfiguration getTypePermissions = builder.Action("GetTypePermissions");
    getTypePermissions.Parameter<string>("typeName");
    getTypePermissions.ReturnsFromEntitySet<TypePermission>("TypePermissions");
    return builder.GetEdmModel();
}
