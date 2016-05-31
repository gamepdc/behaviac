/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Tencent is pleased to support the open source community by making behaviac available.
//
// Copyright (C) 2015 THL A29 Limited, a Tencent company. All rights reserved.
//
// Licensed under the BSD 3-Clause License (the "License"); you may not use this file except in compliance with
// the License. You may obtain a copy of the License at http://opensource.org/licenses/BSD-3-Clause
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;

namespace behaviac
{
    public interface IValue
    {
    }

    public class TValue<T> : IValue
    {
        public T value;

        public TValue(T v)
        {
            Utils.Clone(ref value, v);
        }

        public TValue(TValue<T> rhs)
        {
            Utils.Clone(ref value, rhs.value);
        }

        public TValue<T> Clone()
        {
            return new TValue<T>(this);
        }
    }

    public interface IInstanceMember
    {
        object GetValueObject(Agent self);

        void SetValue(Agent self, object value);

        void SetValue(Agent self, IInstanceMember right);

        bool Compare(Agent self, IInstanceMember right, EOperatorType comparisonType);

        void Compute(Agent self, IInstanceMember right1, IInstanceMember right2, EOperatorType computeType);
    }

    public interface IMember
    {
        IInstanceMember CreateInstance(string instance, IInstanceMember indexMember);
    }

    public interface IProperty : IMember
    {
        void SetValue(Agent self, IInstanceMember right);

        object GetValueObject(Agent self);

        object GetValueObject(Agent self, int index);

#if !BEHAVIAC_RELEASE
        string Name { get; }

        bool IsArrayItem();

        void Log(Agent self);
#endif
    }

    public class CInstanceMember<T> : IInstanceMember
    {
        protected string _instance = "Self";
        protected IInstanceMember _indexMember = null;

        public CInstanceMember()
        {
            _indexMember = null;
        }

        public CInstanceMember(string instance, IInstanceMember indexMember)
        {
            _instance = instance;
            _indexMember = indexMember;
        }

        public CInstanceMember(CInstanceMember<T> rhs)
        {
            _instance = rhs._instance;
            _indexMember = rhs._indexMember;
        }

        public virtual T GetValue(Agent self)
        {
            Debug.Check(false);
            return default(T);
        }

        public object GetValueObject(Agent self)
        {
            return GetValue(self);
        }

        public virtual void SetValue(Agent self, T value)
        {
            Debug.Check(false);
        }

        public void SetValue(Agent self, object value)
        {
            Debug.Check(value == null || !value.GetType().IsValueType);

            SetValue(self, (T)value);
        }

        public void SetValue(Agent self, IInstanceMember right)
        {
            SetValue(self, (CInstanceMember<T>)right);
        }

        public void SetValue(Agent self, CInstanceMember<T> right)
        {
            SetValue(self, right.GetValue(self));
        }

        public bool Compare(Agent self, IInstanceMember right, EOperatorType comparisonType)
        {
            T leftValue = this.GetValue(self);
            T rightValue = ((CInstanceMember<T>)right).GetValue(self);

            return OperationUtils.Compare(leftValue, rightValue, comparisonType);
        }

        public void Compute(Agent self, IInstanceMember right1, IInstanceMember right2, EOperatorType computeType)
        {
            T rightValue1 = ((CInstanceMember<T>)right1).GetValue(self);
            T rightValue2 = ((CInstanceMember<T>)right2).GetValue(self);

            SetValue(self, OperationUtils.Compute(rightValue1, rightValue2, computeType));
        }
    }

    public class CMember<T> : IMember
    {
        public virtual IInstanceMember CreateInstance(string instance, IInstanceMember indexMember)
        {
            Debug.Check(false);
            return null;
        }

        public void SetValue(Agent self, IInstanceMember right)
        {
            T rightValue = ((CInstanceMember<T>)right).GetValue(self);

            SetValue(self, rightValue);
        }

        public virtual void SetValue(Agent self, T value)
        {
            Debug.Check(false);
        }

        public virtual void SetValue(Agent self, T value, int index)
        {
            Debug.Check(false);
        }

        public virtual T GetValue(Agent self)
        {
            Debug.Check(false);
            return default(T);
        }

        public virtual T GetValue(Agent self, int index)
        {
            Debug.Check(false);
            return default(T);
        }
    }

    public class CProperty<T> : CMember<T>, IProperty
    {
        string _name;
        public string Name
        {
            get { return _name; }
        }

#if !BEHAVIAC_RELEASE
        public virtual bool IsArrayItem()
        {
            return false;
        }
#endif

        public CProperty(string name)
        {
#if !BEHAVIAC_RELEASE
            _name = name;
#endif
        }

        public override IInstanceMember CreateInstance(string instance, IInstanceMember indexMember)
        {
            return new CInstanceProperty<T>(instance, indexMember, this);
        }

        public object GetValueObject(Agent self)
        {
            return GetValue(self);
        }

        public object GetValueObject(Agent self, int index)
        {
            return GetValue(self, index);
        }

#if !BEHAVIAC_RELEASE
        public void Log(Agent self)
        {
            if (!this.IsArrayItem())
            {
                uint id = Utils.MakeVariableId(this.Name);
                T value = this.GetValue(self);
                T preValue;
                bool isValid = self.GetPropertyValue(id, out preValue);

                if (!isValid || OperationUtils.Compare<T>(value, preValue, EOperatorType.E_NOTEQUAL))
                {
                    LogManager.Instance.LogVarValue(self, this.Name, value);
                    self.SetPropertyValue(id, value);
                }
            }
        }
#endif
    }

    public class CInstanceProperty<T> : CInstanceMember<T>
    {
        CProperty<T> _property;

        public CInstanceProperty(string instance, IInstanceMember indexMember, CProperty<T> prop)
            : base(instance, indexMember)
        {
            _property = prop;
        }

        public override T GetValue(Agent self)
        {
            Agent agent = Utils.GetParentAgent(self, _instance);

            if (_indexMember != null)
            {
                int indexValue = ((CInstanceMember<int>)_indexMember).GetValue(self);
                return _property.GetValue(agent, indexValue);
            }

            return _property.GetValue(agent);
        }

        public override void SetValue(Agent self, T value)
        {
            Agent agent = Utils.GetParentAgent(self, _instance);

            if (_indexMember != null)
            {
                int indexValue = ((CInstanceMember<int>)_indexMember).GetValue(self);
                _property.SetValue(agent, value, indexValue);
            }
            else
            {
                _property.SetValue(agent, value);
            }
        }
    }

    public class CStaticMemberProperty<T> : CProperty<T>
    {
        public delegate void SetFunctionPointer(T v);
        public delegate T GetFunctionPointer();

        SetFunctionPointer _sfp;
        GetFunctionPointer _gfp;

        public CStaticMemberProperty(string name, SetFunctionPointer sfp, GetFunctionPointer gfp)
            : base(name)
        {
            _sfp = sfp;
            _gfp = gfp;
        }

        public override T GetValue(Agent self)
        {
            Debug.Check(_gfp != null);

            return _gfp();
        }

        public override void SetValue(Agent self, T value)
        {
            Debug.Check(_sfp != null);

            _sfp(value);
        }
    }

    public class CStaticMemberArrayItemProperty<T> : CProperty<T>
    {
        public delegate void SetFunctionPointer(T v, int index);
        public delegate T GetFunctionPointer(int index);

        SetFunctionPointer _sfp;
        GetFunctionPointer _gfp;

        public CStaticMemberArrayItemProperty(string name, SetFunctionPointer sfp, GetFunctionPointer gfp)
            : base(name)
        {
            _sfp = sfp;
            _gfp = gfp;
        }

#if !BEHAVIAC_RELEASE
        public override bool IsArrayItem()
        {
            return true;
        }
#endif
        public override T GetValue(Agent self, int index)
        {
            Debug.Check(_gfp != null);

            return _gfp(index);
        }

        public override void SetValue(Agent self, T value, int index)
        {
            Debug.Check(_sfp != null);

            _sfp(value, index);
        }
    }

    public class CMemberProperty<T> : CProperty<T>
    {
        public delegate void SetFunctionPointer(Agent a, T v);
        public delegate T GetFunctionPointer(Agent a);

        SetFunctionPointer _sfp;
        GetFunctionPointer _gfp;

        public CMemberProperty(string name, SetFunctionPointer sfp, GetFunctionPointer gfp)
            : base(name)
        {
            _sfp = sfp;
            _gfp = gfp;
        }

        public override T GetValue(Agent self)
        {
            Debug.Check(_gfp != null);

            return _gfp(self);
        }

        public override void SetValue(Agent self, T value)
        {
            Debug.Check(_sfp != null);

            _sfp(self, value);
        }
    }

    public class CMemberArrayItemProperty<T> : CProperty<T>
    {
        public delegate void SetFunctionPointer(Agent a, T v, int index);
        public delegate T GetFunctionPointer(Agent a, int index);

        SetFunctionPointer _sfp;
        GetFunctionPointer _gfp;

        public CMemberArrayItemProperty(string name, SetFunctionPointer sfp, GetFunctionPointer gfp)
            : base(name)
        {
            _sfp = sfp;
            _gfp = gfp;
        }

#if !BEHAVIAC_RELEASE
        public override bool IsArrayItem()
        {
            return true;
        }
#endif

        public override T GetValue(Agent self, int index)
        {
            Debug.Check(_gfp != null);

            return _gfp(self, index);
        }

        public override void SetValue(Agent self, T value, int index)
        {
            Debug.Check(_sfp != null);

            _sfp(self, value, index);
        }
    }

    public interface ICustomizedProperty : IProperty
    {
        IInstantiatedVariable Instantiate();
    }

    public interface IInstantiatedVariable
    {
        object GetValueObject(Agent self);

        object GetValueObject(Agent self, int index);

        void SetValue(Agent self, object value);

        void SetValue(Agent self, object value, int index);

#if !BEHAVIAC_RELEASE
        void Log(Agent self);
#endif
    }

    public class CCustomizedProperty<T> : CProperty<T>, ICustomizedProperty
    {
        uint _id;
        T _defaultValue;

        public CCustomizedProperty(uint id, string name, string valueStr)
            : base(name)
        {
            _id = id;
            ValueConverter<T>.Convert(valueStr, out _defaultValue);
        }

        public override T GetValue(Agent self)
        {
            return self.GetVariable<T>(_id);
        }

        public override void SetValue(Agent self, T value)
        {
            self.SetVariable<T>("", _id, value);
        }

        public IInstantiatedVariable Instantiate()
        {
            T value = default(T);
            Utils.Clone(ref value, _defaultValue);
            return new CVariable<T>(this.Name, value);
        }
    }

    public class CCustomizedArrayItemProperty<T> : CProperty<T>, ICustomizedProperty
    {
        uint _parentId;

        public CCustomizedArrayItemProperty(uint parentId, string parentName)
            : base(parentName)
        {
            _parentId = parentId;
        }

#if !BEHAVIAC_RELEASE
        public override bool IsArrayItem()
        {
            return true;
        }
#endif

        public override T GetValue(Agent self, int index)
        {
            List<T> arrayValue = self.GetVariable<List<T>>(_parentId);
            Debug.Check(arrayValue != null);

            return arrayValue[index];
        }

        public override void SetValue(Agent self, T value, int index)
        {
            List<T> arrayValue = self.GetVariable<List<T>>(_parentId);
            Debug.Check(arrayValue != null);

            arrayValue[index] = value;
        }

        public IInstantiatedVariable Instantiate()
        {
            return new CArrayItemVariable<T>(_parentId, this.Name);
        }
    }

    public class CVariable<T> : IInstantiatedVariable
    {
        T _value;

#if !BEHAVIAC_RELEASE
        string _name;

        bool _isModified = false;
        internal bool IsModified
        {
            set { _isModified = value; }
        }
#endif

        public CVariable(string name, T value)
        {
            _value = value;

#if !BEHAVIAC_RELEASE
            _name = name;
#endif
        }

        public CVariable(string name, string valueStr)
        {
            ValueConverter<T>.Convert(valueStr, out _value);

#if !BEHAVIAC_RELEASE
            _name = name;
#endif
        }

        public T GetValue(Agent self)
        {
            return _value;
        }

        public object GetValueObject(Agent self)
        {
            return _value;
        }

        public object GetValueObject(Agent self, int index)
        {
            IList values = _value as IList;
            return (values != null) ? values[index] : _value;
        }

        public void SetValue(Agent self, T value)
        {
            _value = value;

#if !BEHAVIAC_RELEASE
            _isModified = true;
#endif
        }

        public void SetValue(Agent self, object value)
        {
            SetValue(self, (T)value);
        }

        public void SetValue(Agent self, object value, int index)
        {
            Debug.Check(false);
        }

#if !BEHAVIAC_RELEASE
        public void Log(Agent self)
        {
            if (_isModified)
            {
                LogManager.Instance.LogVarValue(self, _name, this.GetValueObject(self));

                _isModified = false;
            }
        }
#endif
    }

    public class CArrayItemVariable<T> : IInstantiatedVariable
    {
        uint _parentId;
        public uint ParentId
        {
            get { return _parentId; }
        }

        public CArrayItemVariable(uint parentId, string parentName)
        {
            _parentId = parentId;
        }

        public T GetValue(Agent self, int index)
        {
            IInstantiatedVariable v = self.GetVar(this.ParentId);

            if (typeof(T).IsValueType)
            {
                CVariable<List<T>> arrayVar = (CVariable<List<T>>)v;
                if (arrayVar != null)
                    return arrayVar.GetValue(self)[index];
            }

            return (T)v.GetValueObject(self, index);
        }

        public void SetValue(Agent self, T value, int index)
        {
            IInstantiatedVariable v = self.GetVar(this.ParentId);
            CVariable<List<T>> arrayVar = (CVariable<List<T>>)v;
            if (arrayVar != null)
            {
                arrayVar.GetValue(self)[index] = value;

#if !BEHAVIAC_RELEASE
                arrayVar.IsModified = true;
#endif
            }
        }

        public object GetValueObject(Agent self)
        {
            Debug.Check(false);
            return null;
        }

        public object GetValueObject(Agent self, int index)
        {
            return GetValue(self, index);
        }

        public void SetValue(Agent self, object value)
        {
            Debug.Check(false);
        }

        public void SetValue(Agent self, object value, int index)
        {
            SetValue(self, (T)value, index);
        }

#if !BEHAVIAC_RELEASE
        public void Log(Agent self)
        {
        }
#endif
    }

    public class CInstanceCustomizedProperty<T> : CInstanceMember<T>
    {
        uint _id;

        public CInstanceCustomizedProperty(string instance, IInstanceMember indexMember, uint id)
            : base(instance, indexMember)
        {
            _id = id;
        }

        public override T GetValue(Agent self)
        {
            if (self != null)
            {
                Agent agent = Utils.GetParentAgent(self, _instance);

                if (_indexMember != null)
                {
                    int indexValue = ((CInstanceMember<int>)_indexMember).GetValue(self);
                    return agent.GetVariable<T>(_id, indexValue);
                }
                else
                {
                    return agent.GetVariable<T>(_id);
                }
            }

            return default(T);
        }

        public override void SetValue(Agent self, T value)
        {
            Agent agent = Utils.GetParentAgent(self, _instance);

            if (_indexMember != null)
            {
                int indexValue = ((CInstanceMember<int>)_indexMember).GetValue(self);
                agent.SetVariable<T>("", _id, value, indexValue);
            }
            else
            {
                agent.SetVariable<T>("", _id, value);
            }
        }
    }

    public class CInstanceConst<T> : CInstanceMember<T>
    {
        T _value;

        public CInstanceConst(object value)
        {
            _value = (T)value;
        }

        public override T GetValue(Agent self)
        {
            return _value;
        }

        public override void SetValue(Agent self, T value)
        {
            _value = value;
        }
    }

    public interface IMethod : IInstanceMember
    {
        IMethod Clone();

        void Load(string instance, string[] paramStrs);

        void Run(Agent self);

        IValue GetIValue(Agent self);

        IValue GetIValue(Agent self, IInstanceMember firstParam);

        void SetTaskParams(Agent self, BehaviorTreeTask treeTask);
    }

    public class CAgentMethodBase<T> : CInstanceMember<T>, IMethod
    {
        protected TValue<T> _returnValue;

        protected CAgentMethodBase()
        {
            _returnValue = new TValue<T>(default(T));
        }

        protected CAgentMethodBase(CAgentMethodBase<T> rhs)
        {
            _returnValue = rhs._returnValue.Clone();
        }

        public virtual IMethod Clone()
        {
            Debug.Check(false);
            return null;
        }

        public virtual void Load(string instance, string[] paramStrs)
        {
            Debug.Check(false);
        }

        public virtual void Run(Agent self)
        {
            Debug.Check(false);
        }

        public override T GetValue(Agent self)
        {
            if (self != null)
                Run(self);

            return _returnValue.value;
        }

        public virtual IValue GetIValue(Agent self)
        {
            if (self != null)
                Run(self);

            return _returnValue;
        }

        public virtual IValue GetIValue(Agent self, IInstanceMember firstParam)
        {
            return GetIValue(self);
        }

        public virtual void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            Debug.Check(false);
        }
    }

    public class CAgentMethod<T> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(Agent a);

        FunctionPointer _fp;

        public CAgentMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethod(CAgentMethod<T> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
        }

        public override IMethod Clone()
        {
            return new CAgentMethod<T>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 0);

            _instance = instance;
        }

        public override void Run(Agent self)
        {
            Agent agent = Utils.GetParentAgent(self, _instance);

            _returnValue.value = _fp(agent);
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
        }
    }

    public class CAgentMethod<T, P1> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(Agent a, P1 p1);

        FunctionPointer _fp;
        IInstanceMember _p1;

        public CAgentMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethod(CAgentMethod<T, P1> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
        }

        public override IMethod Clone()
        {
            return new CAgentMethod<T, P1>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 1);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _returnValue.value = _fp(agent, ((CInstanceMember<P1>)_p1).GetValue(self));
        }

        public override IValue GetIValue(Agent self, IInstanceMember firstParam)
        {
            Debug.Check(_p1 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _returnValue.value = _fp(agent, ((CInstanceMember<P1>)firstParam).GetValue(self));

            return _returnValue;
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));
        }
    }

    public class CAgentMethod<T, P1, P2> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(Agent a, P1 p1, P2 p2);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;

        public CAgentMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethod(CAgentMethod<T, P1, P2> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
        }

        public override IMethod Clone()
        {
            return new CAgentMethod<T, P1, P2>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 2);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _returnValue.value = _fp(agent,
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));
        }
    }

    public class CAgentMethod<T, P1, P2, P3> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(Agent a, P1 p1, P2 p2, P3 p3);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;

        public CAgentMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethod(CAgentMethod<T, P1, P2, P3> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
        }

        public override IMethod Clone()
        {
            return new CAgentMethod<T, P1, P2, P3>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 3);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _returnValue.value = _fp(agent,
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));
        }
    }

    public class CAgentMethod<T, P1, P2, P3, P4> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(Agent a, P1 p1, P2 p2, P3 p3, P4 p4);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;

        public CAgentMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethod(CAgentMethod<T, P1, P2, P3, P4> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
        }

        public override IMethod Clone()
        {
            return new CAgentMethod<T, P1, P2, P3, P4>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 4);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _returnValue.value = _fp(agent,
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));
        }
    }

    public class CAgentMethod<T, P1, P2, P3, P4, P5> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(Agent a, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;

        public CAgentMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethod(CAgentMethod<T, P1, P2, P3, P4, P5> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
        }

        public override IMethod Clone()
        {
            return new CAgentMethod<T, P1, P2, P3, P4, P5>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 5);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _returnValue.value = _fp(agent,
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));
        }
    }

    public class CAgentMethod<T, P1, P2, P3, P4, P5, P6> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(Agent a, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;
        IInstanceMember _p6;

        public CAgentMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethod(CAgentMethod<T, P1, P2, P3, P4, P5, P6> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
            _p6 = rhs._p6;
        }

        public override IMethod Clone()
        {
            return new CAgentMethod<T, P1, P2, P3, P4, P5, P6>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 6);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
            _p6 = AgentMeta.ParseProperty<P6>(paramStrs[5]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);
            Debug.Check(_p6 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _returnValue.value = _fp(agent,
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self),
                ((CInstanceMember<P6>)_p6).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 5);
            treeTask.SetVariable(paramName, ((CInstanceMember<P6>)_p6).GetValue(self));
        }
    }

    public class CAgentMethod<T, P1, P2, P3, P4, P5, P6, P7> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(Agent a, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;
        IInstanceMember _p6;
        IInstanceMember _p7;

        public CAgentMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethod(CAgentMethod<T, P1, P2, P3, P4, P5, P6, P7> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
            _p6 = rhs._p6;
            _p7 = rhs._p7;
        }

        public override IMethod Clone()
        {
            return new CAgentMethod<T, P1, P2, P3, P4, P5, P6, P7>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 7);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
            _p6 = AgentMeta.ParseProperty<P6>(paramStrs[5]);
            _p7 = AgentMeta.ParseProperty<P7>(paramStrs[6]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);
            Debug.Check(_p6 != null);
            Debug.Check(_p7 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _returnValue.value = _fp(agent,
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self),
                ((CInstanceMember<P6>)_p6).GetValue(self),
                ((CInstanceMember<P7>)_p7).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 5);
            treeTask.SetVariable(paramName, ((CInstanceMember<P6>)_p6).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 6);
            treeTask.SetVariable(paramName, ((CInstanceMember<P7>)_p7).GetValue(self));
        }
    }

    public class CAgentMethod<T, P1, P2, P3, P4, P5, P6, P7, P8> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(Agent a, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;
        IInstanceMember _p6;
        IInstanceMember _p7;
        IInstanceMember _p8;

        public CAgentMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethod(CAgentMethod<T, P1, P2, P3, P4, P5, P6, P7, P8> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
            _p6 = rhs._p6;
            _p7 = rhs._p7;
            _p8 = rhs._p8;
        }

        public override IMethod Clone()
        {
            return new CAgentMethod<T, P1, P2, P3, P4, P5, P6, P7, P8>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 8);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
            _p6 = AgentMeta.ParseProperty<P6>(paramStrs[5]);
            _p7 = AgentMeta.ParseProperty<P7>(paramStrs[6]);
            _p8 = AgentMeta.ParseProperty<P8>(paramStrs[7]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);
            Debug.Check(_p6 != null);
            Debug.Check(_p7 != null);
            Debug.Check(_p8 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _returnValue.value = _fp(agent,
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self),
                ((CInstanceMember<P6>)_p6).GetValue(self),
                ((CInstanceMember<P7>)_p7).GetValue(self),
                ((CInstanceMember<P8>)_p8).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 5);
            treeTask.SetVariable(paramName, ((CInstanceMember<P6>)_p6).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 6);
            treeTask.SetVariable(paramName, ((CInstanceMember<P7>)_p7).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 7);
            treeTask.SetVariable(paramName, ((CInstanceMember<P8>)_p8).GetValue(self));
        }
    }

    public class CAgentStaticMethod<T> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer();

        FunctionPointer _fp;

        public CAgentStaticMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethod(CAgentStaticMethod<T> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethod<T>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 0);

            _instance = instance;
        }

        public override void Run(Agent self)
        {
            _returnValue.value = _fp();
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
        }
    }

    public class CAgentStaticMethod<T, P1> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(P1 p1);

        FunctionPointer _fp;
        IInstanceMember _p1;

        public CAgentStaticMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethod(CAgentStaticMethod<T, P1> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethod<T, P1>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 1);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);

            _returnValue.value = _fp(((CInstanceMember<P1>)_p1).GetValue(self));
        }

        public override IValue GetIValue(Agent self, IInstanceMember firstParam)
        {
            Debug.Check(_p1 != null);

            _returnValue.value = _fp(((CInstanceMember<P1>)firstParam).GetValue(self));

            return _returnValue;
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));
        }
    }

    public class CAgentStaticMethod<T, P1, P2> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(P1 p1, P2 p2);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;

        public CAgentStaticMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethod(CAgentStaticMethod<T, P1, P2> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethod<T, P1, P2>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 2);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);

            _returnValue.value = _fp(((CInstanceMember<P1>)_p1).GetValue(self), ((CInstanceMember<P2>)_p2).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));
        }
    }

    public class CAgentStaticMethod<T, P1, P2, P3> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(P1 p1, P2 p2, P3 p3);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;

        public CAgentStaticMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethod(CAgentStaticMethod<T, P1, P2, P3> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethod<T, P1, P2, P3>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 3);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);

            _returnValue.value = _fp(
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));
        }
    }

    public class CAgentStaticMethod<T, P1, P2, P3, P4> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(P1 p1, P2 p2, P3 p3, P4 p4);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;

        public CAgentStaticMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethod(CAgentStaticMethod<T, P1, P2, P3, P4> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethod<T, P1, P2, P3, P4>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 4);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);

            _returnValue.value = _fp(
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));
        }
    }

    public class CAgentStaticMethod<T, P1, P2, P3, P4, P5> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;

        public CAgentStaticMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethod(CAgentStaticMethod<T, P1, P2, P3, P4, P5> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethod<T, P1, P2, P3, P4, P5>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 5);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);

            _returnValue.value = _fp(
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));
        }
    }

    public class CAgentStaticMethod<T, P1, P2, P3, P4, P5, P6> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;
        IInstanceMember _p6;

        public CAgentStaticMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethod(CAgentStaticMethod<T, P1, P2, P3, P4, P5, P6> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
            _p6 = rhs._p6;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethod<T, P1, P2, P3, P4, P5, P6>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 6);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
            _p6 = AgentMeta.ParseProperty<P6>(paramStrs[5]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);
            Debug.Check(_p6 != null);

            _returnValue.value = _fp(
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self),
                ((CInstanceMember<P6>)_p6).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 5);
            treeTask.SetVariable(paramName, ((CInstanceMember<P6>)_p6).GetValue(self));
        }
    }

    public class CAgentStaticMethod<T, P1, P2, P3, P4, P5, P6, P7> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;
        IInstanceMember _p6;
        IInstanceMember _p7;

        public CAgentStaticMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethod(CAgentStaticMethod<T, P1, P2, P3, P4, P5, P6, P7> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
            _p6 = rhs._p6;
            _p7 = rhs._p7;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethod<T, P1, P2, P3, P4, P5, P6, P7>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 7);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
            _p6 = AgentMeta.ParseProperty<P6>(paramStrs[5]);
            _p7 = AgentMeta.ParseProperty<P7>(paramStrs[6]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);
            Debug.Check(_p6 != null);
            Debug.Check(_p7 != null);

            _returnValue.value = _fp(
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self),
                ((CInstanceMember<P6>)_p6).GetValue(self),
                ((CInstanceMember<P7>)_p7).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 5);
            treeTask.SetVariable(paramName, ((CInstanceMember<P6>)_p6).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 6);
            treeTask.SetVariable(paramName, ((CInstanceMember<P7>)_p7).GetValue(self));
        }
    }

    public class CAgentStaticMethod<T, P1, P2, P3, P4, P5, P6, P7, P8> : CAgentMethodBase<T>
    {
        public delegate T FunctionPointer(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;
        IInstanceMember _p6;
        IInstanceMember _p7;
        IInstanceMember _p8;

        public CAgentStaticMethod(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethod(CAgentStaticMethod<T, P1, P2, P3, P4, P5, P6, P7, P8> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
            _p6 = rhs._p6;
            _p7 = rhs._p7;
            _p8 = rhs._p8;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethod<T, P1, P2, P3, P4, P5, P6, P7, P8>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 8);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
            _p6 = AgentMeta.ParseProperty<P6>(paramStrs[5]);
            _p7 = AgentMeta.ParseProperty<P7>(paramStrs[6]);
            _p8 = AgentMeta.ParseProperty<P8>(paramStrs[7]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);
            Debug.Check(_p6 != null);
            Debug.Check(_p7 != null);
            Debug.Check(_p8 != null);

            _returnValue.value = _fp(
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self),
                ((CInstanceMember<P6>)_p6).GetValue(self),
                ((CInstanceMember<P7>)_p7).GetValue(self),
                ((CInstanceMember<P8>)_p8).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 5);
            treeTask.SetVariable(paramName, ((CInstanceMember<P6>)_p6).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 6);
            treeTask.SetVariable(paramName, ((CInstanceMember<P7>)_p7).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 7);
            treeTask.SetVariable(paramName, ((CInstanceMember<P8>)_p8).GetValue(self));
        }
    }

    public class CAgentMethodVoidBase : IMethod
    {
        protected string _instance = "Self";

        public CAgentMethodVoidBase()
        {
        }

        public CAgentMethodVoidBase(CAgentMethodVoidBase rhs)
        {
            _instance = rhs._instance;
        }

        public virtual IMethod Clone()
        {
            Debug.Check(false);
            return null;
        }

        public virtual void Load(string instance, string[] paramStrs)
        {
            Debug.Check(false);

            _instance = instance;
        }

        public virtual void Run(Agent self)
        {
            Debug.Check(false);
        }

        public IValue GetIValue(Agent self)
        {
            Debug.Check(false);
            return null;
        }

        public object GetValueObject(Agent self)
        {
            Debug.Check(false);
            return null;
        }

        public IValue GetIValue(Agent self, IInstanceMember firstParam)
        {
            return GetIValue(self);
        }

        public void SetValue(Agent self, IValue value)
        {
            Debug.Check(false);
        }

        public void SetValue(Agent self, object value)
        {
            Debug.Check(false);
        }

        public void SetValue(Agent self, IInstanceMember right)
        {
            Debug.Check(false);
        }

        public bool Compare(Agent self, IInstanceMember right, EOperatorType comparisonType)
        {
            Debug.Check(false);
            return false;
        }

        public void Compute(Agent self, IInstanceMember right1, IInstanceMember right2, EOperatorType computeType)
        {
            Debug.Check(false);
        }

        public virtual void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            Debug.Check(false);
        }
    }

    public class CAgentMethodVoid : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(Agent a);

        FunctionPointer _fp;

        public CAgentMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethodVoid(CAgentMethodVoid rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
        }

        public override IMethod Clone()
        {
            return new CAgentMethodVoid(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 0);

            _instance = instance;
        }

        public override void Run(Agent self)
        {
            Agent agent = Utils.GetParentAgent(self, _instance);

            _fp(agent);
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
        }
    }

    public class CAgentMethodVoid<P1> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(Agent a, P1 p1);

        FunctionPointer _fp;
        IInstanceMember _p1;

        public CAgentMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethodVoid(CAgentMethodVoid<P1> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
        }

        public override IMethod Clone()
        {
            return new CAgentMethodVoid<P1>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 1);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _fp(agent, ((CInstanceMember<P1>)_p1).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));
        }
    }

    public class CAgentMethodVoid<P1, P2> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(Agent a, P1 p1, P2 p2);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;

        public CAgentMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethodVoid(CAgentMethodVoid<P1, P2> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
        }

        public override IMethod Clone()
        {
            return new CAgentMethodVoid<P1, P2>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 2);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2!= null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _fp(agent, ((CInstanceMember<P1>)_p1).GetValue(self), ((CInstanceMember<P2>)_p2).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));
        }
    }

    public class CAgentMethodVoid<P1, P2, P3> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(Agent a, P1 p1, P2 p2, P3 p3);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;

        public CAgentMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethodVoid(CAgentMethodVoid<P1, P2, P3> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
        }

        public override IMethod Clone()
        {
            return new CAgentMethodVoid<P1, P2, P3>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 3);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _fp(agent,
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));
        }
    }

    public class CAgentMethodVoid<P1, P2, P3, P4> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(Agent a, P1 p1, P2 p2, P3 p3, P4 p4);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;

        public CAgentMethodVoid(FunctionPointer f, IInstanceMember p1, IInstanceMember p2, IInstanceMember p3, IInstanceMember p4)
        {
            _fp = f;
        }

        public CAgentMethodVoid(CAgentMethodVoid<P1, P2, P3, P4> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
        }

        public override IMethod Clone()
        {
            return new CAgentMethodVoid<P1, P2, P3, P4>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 4);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _fp(agent,
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));
        }
    }

    public class CAgentMethodVoid<P1, P2, P3, P4, P5> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(Agent a, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;

        public CAgentMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethodVoid(CAgentMethodVoid<P1, P2, P3, P4, P5> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
        }

        public override IMethod Clone()
        {
            return new CAgentMethodVoid<P1, P2, P3, P4, P5>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 5);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _fp(agent,
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));
        }
    }

    public class CAgentMethodVoid<P1, P2, P3, P4, P5, P6> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(Agent a, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;
        IInstanceMember _p6;

        public CAgentMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethodVoid(CAgentMethodVoid<P1, P2, P3, P4, P5, P6> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
            _p6 = rhs._p6;
        }

        public override IMethod Clone()
        {
            return new CAgentMethodVoid<P1, P2, P3, P4, P5, P6>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 6);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
            _p6 = AgentMeta.ParseProperty<P6>(paramStrs[5]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);
            Debug.Check(_p6 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _fp(agent,
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self),
                ((CInstanceMember<P6>)_p6).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 5);
            treeTask.SetVariable(paramName, ((CInstanceMember<P6>)_p6).GetValue(self));
        }
    }

    public class CAgentMethodVoid<P1, P2, P3, P4, P5, P6, P7> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(Agent a, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;
        IInstanceMember _p6;
        IInstanceMember _p7;

        public CAgentMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethodVoid(CAgentMethodVoid<P1, P2, P3, P4, P5, P6, P7> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
            _p6 = rhs._p6;
            _p7 = rhs._p7;
        }

        public override IMethod Clone()
        {
            return new CAgentMethodVoid<P1, P2, P3, P4, P5, P6, P7>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 7);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
            _p6 = AgentMeta.ParseProperty<P6>(paramStrs[5]);
            _p7 = AgentMeta.ParseProperty<P7>(paramStrs[6]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);
            Debug.Check(_p6 != null);
            Debug.Check(_p7 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _fp(agent,
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self),
                ((CInstanceMember<P6>)_p6).GetValue(self),
                ((CInstanceMember<P7>)_p7).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 5);
            treeTask.SetVariable(paramName, ((CInstanceMember<P6>)_p6).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 6);
            treeTask.SetVariable(paramName, ((CInstanceMember<P7>)_p7).GetValue(self));
        }
    }

    public class CAgentMethodVoid<P1, P2, P3, P4, P5, P6, P7, P8> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(Agent a, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;
        IInstanceMember _p6;
        IInstanceMember _p7;
        IInstanceMember _p8;

        public CAgentMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentMethodVoid(CAgentMethodVoid<P1, P2, P3, P4, P5, P6, P7, P8> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
            _p6 = rhs._p6;
            _p7 = rhs._p7;
            _p8 = rhs._p8;
        }

        public override IMethod Clone()
        {
            return new CAgentMethodVoid<P1, P2, P3, P4, P5, P6, P7, P8>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 8);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
            _p6 = AgentMeta.ParseProperty<P6>(paramStrs[5]);
            _p7 = AgentMeta.ParseProperty<P7>(paramStrs[6]);
            _p8 = AgentMeta.ParseProperty<P8>(paramStrs[7]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);
            Debug.Check(_p6 != null);
            Debug.Check(_p7 != null);
            Debug.Check(_p8 != null);

            Agent agent = Utils.GetParentAgent(self, _instance);

            _fp(agent,
                ((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self),
                ((CInstanceMember<P6>)_p6).GetValue(self),
                ((CInstanceMember<P7>)_p7).GetValue(self),
                ((CInstanceMember<P8>)_p8).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 5);
            treeTask.SetVariable(paramName, ((CInstanceMember<P6>)_p6).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 6);
            treeTask.SetVariable(paramName, ((CInstanceMember<P7>)_p7).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 7);
            treeTask.SetVariable(paramName, ((CInstanceMember<P8>)_p8).GetValue(self));
        }
    }

    public class CAgentStaticMethodVoid : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer();

        FunctionPointer _fp;

        public CAgentStaticMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethodVoid(CAgentStaticMethodVoid rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethodVoid(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 0);

            _instance = instance;
        }

        public override void Run(Agent self)
        {
            _fp();
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
        }
    }

    public class CAgentStaticMethodVoid<P1> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(P1 p1);

        FunctionPointer _fp;
        IInstanceMember _p1;

        public CAgentStaticMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethodVoid(CAgentStaticMethodVoid<P1> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethodVoid<P1>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 1);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);

            _fp(((CInstanceMember<P1>)_p1).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));
        }
    }

    public class CAgentStaticMethodVoid<P1, P2> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(P1 p1, P2 p2);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;

        public CAgentStaticMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethodVoid(CAgentStaticMethodVoid<P1, P2> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethodVoid<P1, P2>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 2);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2!= null);

            _fp(((CInstanceMember<P1>)_p1).GetValue(self), ((CInstanceMember<P2>)_p2).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));
        }
    }

    public class CAgentStaticMethodVoid<P1, P2, P3> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(P1 p1, P2 p2, P3 p3);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;

        public CAgentStaticMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethodVoid(CAgentStaticMethodVoid<P1, P2, P3> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethodVoid<P1, P2, P3>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 3);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);

            _fp(((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));
        }
    }

    public class CAgentStaticMethodVoid<P1, P2, P3, P4> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(P1 p1, P2 p2, P3 p3, P4 p4);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;

        public CAgentStaticMethodVoid(FunctionPointer f, IInstanceMember p1, IInstanceMember p2, IInstanceMember p3, IInstanceMember p4)
        {
            _fp = f;
        }

        public CAgentStaticMethodVoid(CAgentStaticMethodVoid<P1, P2, P3, P4> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethodVoid<P1, P2, P3, P4>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 4);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);

            _fp(((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));
        }
    }

    public class CAgentStaticMethodVoid<P1, P2, P3, P4, P5> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;

        public CAgentStaticMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethodVoid(CAgentStaticMethodVoid<P1, P2, P3, P4, P5> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethodVoid<P1, P2, P3, P4, P5>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 5);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);

            _fp(((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));
        }
    }

    public class CAgentStaticMethodVoid<P1, P2, P3, P4, P5, P6> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;
        IInstanceMember _p6;

        public CAgentStaticMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethodVoid(CAgentStaticMethodVoid<P1, P2, P3, P4, P5, P6> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
            _p6 = rhs._p6;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethodVoid<P1, P2, P3, P4, P5, P6>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 6);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
            _p6 = AgentMeta.ParseProperty<P6>(paramStrs[5]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);
            Debug.Check(_p6 != null);

            _fp(((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self),
                ((CInstanceMember<P6>)_p6).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 5);
            treeTask.SetVariable(paramName, ((CInstanceMember<P6>)_p6).GetValue(self));
        }
    }

    public class CAgentStaticMethodVoid<P1, P2, P3, P4, P5, P6, P7> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;
        IInstanceMember _p6;
        IInstanceMember _p7;

        public CAgentStaticMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethodVoid(CAgentStaticMethodVoid<P1, P2, P3, P4, P5, P6, P7> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
            _p6 = rhs._p6;
            _p7 = rhs._p7;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethodVoid<P1, P2, P3, P4, P5, P6, P7>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 7);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
            _p6 = AgentMeta.ParseProperty<P6>(paramStrs[5]);
            _p7 = AgentMeta.ParseProperty<P7>(paramStrs[6]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);
            Debug.Check(_p6 != null);
            Debug.Check(_p7 != null);

            _fp(((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self),
                ((CInstanceMember<P6>)_p6).GetValue(self),
                ((CInstanceMember<P7>)_p7).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 5);
            treeTask.SetVariable(paramName, ((CInstanceMember<P6>)_p6).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 6);
            treeTask.SetVariable(paramName, ((CInstanceMember<P7>)_p7).GetValue(self));
        }
    }

    public class CAgentStaticMethodVoid<P1, P2, P3, P4, P5, P6, P7, P8> : CAgentMethodVoidBase
    {
        public delegate void FunctionPointer(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8);

        FunctionPointer _fp;
        IInstanceMember _p1;
        IInstanceMember _p2;
        IInstanceMember _p3;
        IInstanceMember _p4;
        IInstanceMember _p5;
        IInstanceMember _p6;
        IInstanceMember _p7;
        IInstanceMember _p8;

        public CAgentStaticMethodVoid(FunctionPointer f)
        {
            _fp = f;
        }

        public CAgentStaticMethodVoid(CAgentStaticMethodVoid<P1, P2, P3, P4, P5, P6, P7, P8> rhs)
            : base(rhs)
        {
            _fp = rhs._fp;
            _p1 = rhs._p1;
            _p2 = rhs._p2;
            _p3 = rhs._p3;
            _p4 = rhs._p4;
            _p5 = rhs._p5;
            _p6 = rhs._p6;
            _p7 = rhs._p7;
            _p8 = rhs._p8;
        }

        public override IMethod Clone()
        {
            return new CAgentStaticMethodVoid<P1, P2, P3, P4, P5, P6, P7, P8>(this);
        }

        public override void Load(string instance, string[] paramStrs)
        {
            Debug.Check(paramStrs.Length == 8);

            _instance = instance;
            _p1 = AgentMeta.ParseProperty<P1>(paramStrs[0]);
            _p2 = AgentMeta.ParseProperty<P2>(paramStrs[1]);
            _p3 = AgentMeta.ParseProperty<P3>(paramStrs[2]);
            _p4 = AgentMeta.ParseProperty<P4>(paramStrs[3]);
            _p5 = AgentMeta.ParseProperty<P5>(paramStrs[4]);
            _p6 = AgentMeta.ParseProperty<P6>(paramStrs[5]);
            _p7 = AgentMeta.ParseProperty<P7>(paramStrs[6]);
            _p8 = AgentMeta.ParseProperty<P8>(paramStrs[7]);
        }

        public override void Run(Agent self)
        {
            Debug.Check(_p1 != null);
            Debug.Check(_p2 != null);
            Debug.Check(_p3 != null);
            Debug.Check(_p4 != null);
            Debug.Check(_p5 != null);
            Debug.Check(_p6 != null);
            Debug.Check(_p7 != null);
            Debug.Check(_p8 != null);

            _fp(((CInstanceMember<P1>)_p1).GetValue(self),
                ((CInstanceMember<P2>)_p2).GetValue(self),
                ((CInstanceMember<P3>)_p3).GetValue(self),
                ((CInstanceMember<P4>)_p4).GetValue(self),
                ((CInstanceMember<P5>)_p5).GetValue(self),
                ((CInstanceMember<P6>)_p6).GetValue(self),
                ((CInstanceMember<P7>)_p7).GetValue(self),
                ((CInstanceMember<P8>)_p8).GetValue(self));
        }

        public override void SetTaskParams(Agent self, BehaviorTreeTask treeTask)
        {
            string paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 0);
            treeTask.SetVariable(paramName, ((CInstanceMember<P1>)_p1).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 1);
            treeTask.SetVariable(paramName, ((CInstanceMember<P2>)_p2).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 2);
            treeTask.SetVariable(paramName, ((CInstanceMember<P3>)_p3).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 3);
            treeTask.SetVariable(paramName, ((CInstanceMember<P4>)_p4).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 4);
            treeTask.SetVariable(paramName, ((CInstanceMember<P5>)_p5).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 5);
            treeTask.SetVariable(paramName, ((CInstanceMember<P6>)_p6).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 6);
            treeTask.SetVariable(paramName, ((CInstanceMember<P7>)_p7).GetValue(self));

            paramName = string.Format("{0}{1}", Task.LOCAL_TASK_PARAM_PRE, 7);
            treeTask.SetVariable(paramName, ((CInstanceMember<P8>)_p8).GetValue(self));
        }
    }

}//namespace behaviac
