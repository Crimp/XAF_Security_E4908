# .NET App Security API Benchmark for EF Core and XPO

## About
This project is a [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet)-based benchmark. We used it to test the performance of the following Object-Relational Mapping (ORM) libraries for .NET Core 5.0 with [XAF's Security System](https://www.devexpress.com/products/net/application_framework/security.xml):
 - [Microsoft Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) (EF Core);
 - [DevExpress eXpress Persistent Objects™](https://www.devexpress.com/Products/NET/ORM/) (XPO).
 
You can run these benchmarks or review our test results below. Needless to say, the lower the execution time the better.

All benchmarks were executed using .NET Core 5.0, AnyCPU release builds (include warm-up), Windows 10 Enterprise x64, local Microsoft SQL Server Express (64-bit) v13.0.4001.0, i7-6700 CPU @4.0GHz / 32GB RAM / SSD. 

## Run Benchmarks in Your Environment

Feel free to make data model and test case modifications to cover additional usage scenarios. For instance,  measure memory consumption, include scenarios with BLOBs, reference and collection properties, etc. 

If you download the project to run benchmark tests in your environment, do the following:
1. Download and run the [DevExpress Unified Component Installer](https://www.devexpress.com/Products/Try/) to add [DevExpress.Xpo](https://www.nuget.org/packages/DevExpress.Xpo/) and other libraries to project references.
2. Edit the connection string in the [App.config](/Benchmarks/XAFSecurityBenchmark/XAFSecurityBenchmark/App.config) file;
3. Optionally update the ORM library and target framework versions, if necessary.

## Get Support
If you cannot compile or run any of these demo apps or have questions about our tutorials or supported functionality, please submit questions in the [Support Center](https://www.devexpress.com/ask) or [the Issues tab above](https://github.com/DevExpress-Examples/XAF_how-to-use-the-integrated-mode-of-the-security-system-in-non-xaf-applications-e4908/issues) - we will be more than happy to help you.

## Benchmark Configuration
### Data Model Structure
This project uses the following business objects:

<p float="left">
  <img src="https://raw.githubusercontent.com/DevExpress-Examples/XAF_Security_E4908/master/Benchmarks/images/ClassDiagram.png"  /> 
</p>

### Security Users and Permissions
To create and load these objects, this benchmark uses the following data access rules:
1. a user can access only the Contacts that have the same Department as the current user;
2. a user can access the Tasks only if its Contacts collection contains a user from the same Department or if the AssignedTo Contact is from the same Department

In code, these rules are implemented in the following manner.

#### In Apps with No Security System

```csharp
    Expression<Func<Contact, bool>> ContactsFilterPredicate(ICustomPermissionPolicyUser currentUser) =>
        contact => contact.Department == currentUser.Department;

    Expression<Func<DemoTask, bool>> TasksFilterPredicate(ICustomPermissionPolicyUser currentUser) =>
        task => task.Contacts.Any(contact => contact.Department.Users.Any(user => user == currentUser)) ||
        ((Contact)task.AssignedTo).Department == currentUser.Department;
```
We are using these filter predicates to load objects for XPO and EF Core tests without security to simulate how it could be done without integrated security to comparison.

#### In Apps with the Security System
```csharp
    userRole.AddTypePermission<ContactType>(SecurityOperations.FullObjectAccess, SecurityPermissionState.Deny);
    userRole.AddObjectPermission<ContactType>(SecurityOperations.FullObjectAccess,
      $"[Department].[Users][[{keyPropertyName}] == CurrentUserId()].Exists()", SecurityPermissionState.Allow);

    userRole.AddTypePermission<TaskType>(SecurityOperations.FullObjectAccess, SecurityPermissionState.Deny);
    userRole.AddObjectPermission<TaskType>(SecurityOperations.FullObjectAccess,
      $"[Contacts][[Department].[Users][[{keyPropertyName}] == CurrentUserId()].Exists()]", SecurityPermissionState.Allow);

    if(typeof(TaskType).IsSubclassOf(typeof(DevExpress.Persistent.BaseImpl.Task))) {
        userRole.AddObjectPermission<TaskType>(SecurityOperations.FullObjectAccess,
          $"[AssignedTo].<Contact>[Department].[Users][[{keyPropertyName}] == CurrentUserId()].Exists()", SecurityPermissionState.Allow);
    }
    else {
        userRole.AddObjectPermission<TaskType>(SecurityOperations.FullObjectAccess,
          "Upcast(AssignedTo, 'XAFSecurityBenchmark.Models.EFCore.Contact', 'Department') == CurrentUserDepartment()", SecurityPermissionState.Allow);
    }
```
 **Source:** [DBUpdaterBase.CreateSecurityObjects](/Benchmarks/XAFSecurityBenchmark/XAFSecurityBenchmark/PerformanceTests/Base/DBUpdater/DBUpdaterBase.cs#L114-131)

### Initial Data
1) Tests that create new objects are executed with an empty database. The database is cleaned after every test iteration cycle.
2) Tests that load collections and modify data work with the following data: 
     - The database updater creates five test users, the [TestSetConfig.Users](/Benchmarks/XAFSecurityBenchmark/XAFSecurityBenchmark/PerformanceTests/Base/TestSetConfig.cs#L20) array is used.
     - For every User, Contacts are created based on the item count for each test specified by the [TestSetConfig.ContactCountPerUserToCreate](/Benchmarks/XAFSecurityBenchmark/XAFSecurityBenchmark/PerformanceTests/Base/TestSetConfig.cs#L21) value.
     - For every Contact, the [TestSetConfig.TasksAssigedWithContact](/Benchmarks/XAFSecurityBenchmark/XAFSecurityBenchmark/PerformanceTests/Base/TestSetConfig.cs#L22) and [TestSetConfig.TasksLinkedWithContact](/Benchmarks/XAFSecurityBenchmark/XAFSecurityBenchmark/PerformanceTests/Base/TestSetConfig.cs#L23) amounts of Tasks is created and linked with the Contact. The database has 25K Contacts and 50OK Tasks.
    - For every Contact, we initialize its reference data such as PhoneNumber, Position and Address.

For more information, see the logic of test object creation in the [TemporaryTestObjectsHelper](/Benchmarks/XAFSecurityBenchmark/XAFSecurityBenchmark/PerformanceTests/Base/DBUpdater/TempDataCreationHelpers/TemporaryTestObjectsHelper.cs) class.


## Test Results

### Scenario #1. Load Contacts
Loads different numbers of Contacts for a specific User.
<p float="left">
  <img src="https://raw.githubusercontent.com/DevExpress-Examples/XAF_Security_E4908/master/Benchmarks/images/getContacts_smallDataSet.svg" width="100%" /> 
  <img src="https://raw.githubusercontent.com/DevExpress-Examples/XAF_Security_E4908/master/Benchmarks/images/getContacts_largeDataSet.svg" width="100%"/>
</p>

|Item Count | EF Core 5 (No Security), ms | EF Core 5 (Security), ms | XPO (No Security), ms | XPO (Security), ms |
|------------|----------------------------|-------------------------|----------------------|-------------------|
|10          |1.370                       |12.734                   |2.781                 |7.149              |
|20          |1.415                       |17.683                   |3.760                 |7.304              |
|50          |1.708                       |26.966                   |5.051                 |12.335             |
|100         |2.017                       |44.743                   |7.246                 |20.567             |
|250         |3.065                       |101.844                  |15.311                |48.055             |
|500         |4.741                       |196.163                  |26.649                |94.178             |
|1000        |8.098                       |407.876                  |53.689                |188.849            |
|2500        |18.643                      |1,139.486                |154.995               |474.939            |
|5000        |19.457                      |2,847.249                |268.775               |967.130            |

**Source:** [XAFSecurityBenchmark.PerformanceTests.PerformanceTestSet.GetContacts](/Benchmarks/XAFSecurityBenchmark/XAFSecurityBenchmark/PerformanceTests/Base/PerformanceTestSet.cs#L87-L89)


### Scenario #2. Load Tasks
Loads different numbers of Tasks for a specific User.

<p float="left">
  <img src="https://raw.githubusercontent.com/DevExpress-Examples/XAF_Security_E4908/master/Benchmarks/images/getTasks_smallDataSet.svg" width="100%"/>
  
  <img src="https://raw.githubusercontent.com/DevExpress-Examples/XAF_Security_E4908/master/Benchmarks/images/getTasks_largeDataSet.svg" width="100%"/> 
</p>

|Item Count | EF Core 5 (No Security), ms | EF Core 5 (Security), ms | XPO (No Security), ms | XPO (Security), ms |
|------------|--------------------------|---------------------------|----------------------|-----------------------|
|10          |1.581                     |26.763                     |4.184                 |38.900                 |
|20          |1.865                     |37.863                     |6.792                 |88.789                 |
|50          |2.449                     |75.082                     |12.080                |202.839                |
|100         |3.284                     |134.820                    |21.896                |416.492                |
|250         |19.132                    |325.692                    |44.621                |1,069.608              |
|500         |22.137                    |634.178                    |79.334                |2,084.031              |
|1000        |28.301                    |1,253.319                  |153.552               |4,192.824              |
|2500        |47.406                    |3,105.556                  |304.600               |10,222.239             |
|5000        |78.365                    |6,198.760                  |558.145               |20,674.707             | 


**Source:** [XAFSecurityBenchmark.PerformanceTests.PerformanceTestSet.GetTasks](/Benchmarks/XAFSecurityBenchmark/XAFSecurityBenchmark/PerformanceTests/Base/PerformanceTestSet.cs#L91-L93)


### Scenario #3. Create a new Contact with Tasks
Creates a new Contact. Creates new PhoneNumbers, Positions, Addresses, and 20 Tasks and links them to the Contact.

<p float="left">
  <img src="https://raw.githubusercontent.com/DevExpress-Examples/XAF_Security_E4908/master/Benchmarks/images/insertContact_smallDataSet.svg" width="100%"/>
  
  <img src="https://raw.githubusercontent.com/DevExpress-Examples/XAF_Security_E4908/master/Benchmarks/images/insertContact_largeDataSet.svg" width="100%"/> 
</p>

|Item Count | EF Core 5 (No Security), ms | EF Core 5 (Security), ms | XPO (No Security), ms | XPO (Security), ms |
|------------|----------------------------|-------------------------|----------------------|-----------------------|
|10          |37.689                      |49.538                   |9.610                 |17.945                 |
|20          |48.919                      |63.285                   |16.315                |30.597                 |
|50          |111.202                     |130.787                  |40.767                |62.213                 |
|100         |191.713                     |219.141                  |76.946                |115.297                |
|250         |323.199                     |377.414                  |188.958               |283.511                |
|500         |519.324                     |612.042                  |375.124               |558.222                |
|1000        |934.478                     |1,101.866                |762.798               |1,143.385              |
|2500        |2,251.004                   |2,706.443                |2,015.533             |3,217.575              |
|5000        |4,434.771                   |5,261.035                |4,336.850             |6,529.839              |

**Source:** [XAFSecurityBenchmark.PerformanceTests.PerformanceTestSet.InsertContact](/Benchmarks/XAFSecurityBenchmark/XAFSecurityBenchmark/PerformanceTests/Base/PerformanceTestSet.cs#L75-L77)


### Scenario #4. Create a new Contact without reference objects
Creates a new Contact without Tasks.

<p float="left">
  <img src="https://raw.githubusercontent.com/DevExpress-Examples/XAF_Security_E4908/master/Benchmarks/images/insertEmptyContact_smallDataSet.svg" width="100%"/>
  
  <img src="https://raw.githubusercontent.com/DevExpress-Examples/XAF_Security_E4908/master/Benchmarks/images/insertEmptyContact_largeDataSet.svg" width="100%"/> 
</p>

|Item Count | EF Core 5 (No Security), ms | EF Core 5 (Security), ms | XPO (No Security), ms | XPO (Security), ms |
|------------|----------------------------|-------------------------|----------------------|-----------------------|
|10          |16.224                      |21.824                   |6.256                 |8.557                  |
|20          |21.386                      |28.582                   |10.476                |12.832                 |
|50          |49.791                      |59.096                   |23.785                |28.845                 |
|100         |67.251                      |82.284                   |44.266                |50.461                 |
|250         |122.076                     |148.043                  |104.173               |123.448                |
|500         |191.099                     |235.686                  |207.360               |242.628                |
|1000        |333.375                     |413.528                  |410.068               |475.639                |
|2500        |776.824                     |949.982                  |1,027.423             |1,216.567              |
|5000        |1,514.629                   |1,864.973                |2,102.060             |2,503.027              |

**Source:** [XAFSecurityBenchmark.PerformanceTests.PerformanceTestSet.InsertEmptyContact](/Benchmarks/XAFSecurityBenchmark/XAFSecurityBenchmark/PerformanceTests/Base/PerformanceTestSet.cs#L71-L73)


### Scenario #5. Load, update, and save Contacts
Loads different numbers of Contacts for a specific User, modifies them, and saves.

<p float="left">
  <img src="https://raw.githubusercontent.com/DevExpress-Examples/XAF_Security_E4908/master/Benchmarks/images/updateContact_smallDataSet.svg" width="100%"/>
  
  <img src="https://raw.githubusercontent.com/DevExpress-Examples/XAF_Security_E4908/master/Benchmarks/images/updateContact_largeDataSet.svg" width="100%"/> 
</p>

|Item Count | EF Core 5 (No Security), ms | EF Core 5 (Security), ms | XPO (No Security), ms | XPO (Security), ms |
|------------|----------------------------|-------------------------|----------------------|-----------------------|
|10          |2.810                       |15.952                   |5.582                 |10.709                 |
|20          |3.857                       |23.704                   |8.366                 |16.588                 |
|50          |6.304                       |35.559                   |15.322                |35.404                 |
|100         |10.674                      |62.368                   |27.150                |60.569                 |
|250         |22.372                      |139.360                  |64.693                |140.151                |
|500         |41.286                      |273.831                  |131.991               |285.815                |
|1000        |83.497                      |559.966                  |246.728               |601.347                |
|2500        |211.406                     |1,539.082                |647.841               |1,603.247              |
|5000        |465.541                     |3,636.916                |1,267.308             |3,158.913              |

**Source:** [XAFSecurityBenchmark.PerformanceTests.PerformanceTestSet.UpdateContacts](/Benchmarks/XAFSecurityBenchmark/XAFSecurityBenchmark/PerformanceTests/Base/PerformanceTestSet.cs#L79-L81)


### Scenario #6. Load, update, and save Tasks
Loads different numbers of Tasks for a specific User, modifies them, and saves.

<p float="left">
  <img src="https://raw.githubusercontent.com/DevExpress-Examples/XAF_Security_E4908/master/Benchmarks/images/updateTask_smallDataSet.svg" width="100%"/>
  
  <img src="https://raw.githubusercontent.com/DevExpress-Examples/XAF_Security_E4908/master/Benchmarks/images/updateTask_largeDataSet.svg" width="100%"/> 
</p>

|Item Count | EF Core 5 (No Security), ms | EF Core 5 (Security), ms | XPO (No Security), ms | XPO (Security), ms |
|------------|----------------------------|-------------------------|----------------------|-----------------------|
|10          |1.475                       |22.537                   |5.841                 |90.147                 |
|20          |1.881                       |37.966                   |8.603                 |169.677                |
|50          |2.813                       |79.589                   |14.698                |407.658                |
|100         |3.662                       |136.937                  |26.857                |794.231                |
|250         |12.763                      |325.457                  |66.651                |1,966.607              |
|500         |18.088                      |647.989                  |113.578               |3,939.288              |
|1000        |25.225                      |1,281.299                |236.457               |7,834.771              |
|2500        |49.258                      |3,161.325                |542.876               |19,379.141             |
|5000        |90.776                      |6,267.947                |1,043.535             |38,638.566             |

**Source:** [XAFSecurityBenchmark.PerformanceTests.PerformanceTestSet.UpdateTasks](/Benchmarks/XAFSecurityBenchmark/XAFSecurityBenchmark/PerformanceTests/Base/PerformanceTestSet.cs#L83-L85)