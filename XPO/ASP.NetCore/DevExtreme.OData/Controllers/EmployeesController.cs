﻿using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Query;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using BusinessObjectsLibrary.BusinessObjects;
using DevExpress.ExpressApp.Core;

namespace DevExtreme.OData.Controllers {
	public class EmployeesController : ODataController, IDisposable {
		readonly IObjectSpace objectSpace;
		public EmployeesController(IObjectSpaceFactory objectSpaceFactory) {
			objectSpace = objectSpaceFactory.CreateObjectSpace<Employee>();
		}
		[HttpGet]
		[EnableQuery]
		public ActionResult Get() {
			// The XPO way:
			// var session = ((SecuredObjectSpace)ObjectSpace).Session;
			// 
			// The XAF way:
			IQueryable<Employee> employees = ((XPQuery<Employee>)objectSpace.GetObjectsQuery<Employee>());
			return Ok(employees);
		}
		[HttpDelete]
		public ActionResult Delete(Guid key) {
			Employee existing = objectSpace.GetObjectByKey<Employee>(key);
			if(existing != null) {
			    objectSpace.Delete(existing);
			    objectSpace.CommitChanges();
			    return NoContent();
			}
            return NotFound(); 
		}
		[HttpPatch]
		public ActionResult Patch(Guid key, [FromBody]JsonElement jElement) {
			Employee employee = objectSpace.FirstOrDefault<Employee>(e => e.Oid == key);
			if(employee != null) {
				JsonParser.ParseJObject<Employee>(jElement, employee, objectSpace);
				return Ok(employee);
			}
			return NotFound();
		}
		[HttpPost]
		public ActionResult Post([FromBody]JsonElement jElement) {
			Employee employee = objectSpace.CreateObject<Employee>();
			JsonParser.ParseJObject<Employee>(jElement, employee, objectSpace);
			return Ok(employee);
		}
		public void Dispose() {
			objectSpace.Dispose();
		}
	}
}
