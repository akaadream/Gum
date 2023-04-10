﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDataUi.DataTypes
{
    public class MultiSelectInstanceMember : InstanceMember
    {
        public override bool IsDefault 
        { 
            get => InstanceMembers.All(item => item.IsDefault); 

            set
            {
                foreach(var item in InstanceMembers)
                {
                    item.IsDefault = value;
                }
            }
        }

        public override bool IsIndeterminate
        {
            get
            {
                if(InstanceMembers.Count() < 2)
                {
                    return false;
                }
                else
                {
                    var firstValue = InstanceMembers[0].Value;
                    foreach (var innerMember in InstanceMembers.Skip(1))
                    {
                        if (!object.Equals(firstValue, innerMember.Value))
                        {
                            return true;
                        }
                    }
                    return false;
                }

            }
        }

        public List<InstanceMember> InstanceMembers { get; set; }

        public MultiSelectInstanceMember() 
        {
            CustomSetEvent += HandleCustomSetEvent;
            CustomGetEvent += HandleCustomGetEvent;
            CustomGetTypeEvent += HandleCustomGetTypeEvent;
            SetValueError = HandleValueError;
        }

        private void HandleCustomSetEvent(object owner, object value)
        {
            foreach(var innerMember in InstanceMembers)
            {
                innerMember.Value = value;
            }
        }

        private object HandleCustomGetEvent(object owner)
        {
            if(InstanceMembers.Count == 0) return null;
            else if (InstanceMembers.Count == 1) return InstanceMembers[0].Value;
            else
            {
                var firstValue = InstanceMembers[0].Value;
                foreach(var innerMember in InstanceMembers.Skip(1))
                {
                    if(!object.Equals(firstValue, innerMember.Value))
                    {
                        return null;
                    }
                }
                return firstValue;
            }
        }

        private Type HandleCustomGetTypeEvent(object arg)
        {
            return InstanceMembers.FirstOrDefault()?.PropertyType;
        }

        private void HandleValueError(object obj)
        {
            foreach(var innerMember in InstanceMembers)
            {
                innerMember.SetValueError(obj);
            }
        }
    }
}
