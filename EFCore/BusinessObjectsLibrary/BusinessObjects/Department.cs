﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BusinessObjectsLibrary.BusinessObjects {
    public class Department {
        public virtual int ID { get; set; }

        public virtual string Title { get; set; }

        public virtual string Office { get; set; }

        public virtual IList<Employee> Employees { get; set; } = new ObservableCollection<Employee>();

        public override string ToString() {
            return Title;
        }
    }
}
