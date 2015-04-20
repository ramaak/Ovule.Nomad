/*
Copyright (c) 2015 Tony Di Nucci (tonydinucci[at]gmail[dot]com)
 
This file is part of Nomad.

Nomad is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Nomad is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Nomad.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Ovule
{
  public class ExpressionUtils
  {
    //code here based on:
    //https://github.com/mgravell/protobuf-net/blob/15174a09ee3223c8805b3ef81c1288879c746dfa/protobuf-net.Extensions/ServiceModel/Client/ProtoClientExtensions.cs
    //TODO: The methods as they are don't feel quite right for a Util class.  Will do for not but see about improving it later.

    public static KeyValuePair<MethodInfo, Tuple<Type, object>[]> ResolveMethod(Expression methodCall)
    {
      if (methodCall == null)
        throw new ArgumentNullException("methodCall");

      LambdaExpression lambda = methodCall as LambdaExpression;
      if (lambda == null)
        throw new ArgumentException("LambdaExpression expected", "methodCall");

      MethodCallExpression call = lambda.Body as MethodCallExpression;
      if (call == null)
        throw new NotSupportedException("Methods must invoked directly");

      Tuple<Type, object>[] args = new Tuple<Type, object>[call.Arguments.Count];
      ParameterInfo[] parameters = call.Method.GetParameters();

      for (int i = 0; i < args.Length; i++)
      {
        if (parameters[i].ParameterType.IsByRef)
          throw new NotSupportedException("Currently 'ref' parameters are not supported");
        else
        {
          Tuple<Type, object> typeVal = Evaluate(call.Arguments[i]);
          args[i] = typeVal;
        }
      }
      return new KeyValuePair<MethodInfo, Tuple<Type, object>[]>(call.Method, args);
    }

    public static Tuple<Type, object> Evaluate(Expression operation)
    {
      Type type;
      object value;
      if (!TryEvaluate(operation, out type, out value))
        throw new NotSupportedException("The system not currently capable of evaluating one or more of the arguments.  Please simplify the call");
      return new Tuple<Type, object>(type, value);
    }

    public static bool TryEvaluate(Expression operation, out Type type, out object value)
    {
      type = null;
      value = null;
      if (operation == null)
      {
        // used for static fields, etc
        return true;
      }

      switch (operation.NodeType)
      {
        case ExpressionType.Convert:
          UnaryExpression unExp = operation as UnaryExpression;
          if (unExp != null)
          {
            type = unExp.Type;
            Type convertedType;
            if (TryEvaluate(((UnaryExpression)operation).Operand, out convertedType, out value))
              return true;
          }
          return false;
        case ExpressionType.Constant:
          type = ((ConstantExpression)operation).Type;
          value = ((ConstantExpression)operation).Value;
          return true;
        case ExpressionType.MemberAccess:
          MemberExpression me = (MemberExpression)operation;
          object target;
          if (TryEvaluate(me.Expression, out type, out target))
          {
            // instance target
            switch (me.Member.MemberType)
            {
              case MemberTypes.Field:
                type = ((FieldInfo)me.Member).FieldType;
                value = ((FieldInfo)me.Member).GetValue(target);
                return true;
              case MemberTypes.Property:
                type = ((PropertyInfo)me.Member).PropertyType;
                value = ((PropertyInfo)me.Member).GetValue(target, null);
                return true;
            }
          }
          break;
        case ExpressionType.Call:
        case ExpressionType.New:
          if (operation is MethodCallExpression)
            type = ((MethodCallExpression)operation).Method.ReturnType;
          else
            type = typeof(void);
          value = Expression.Lambda(operation).Compile().DynamicInvoke();
          return true;
      }
      return false;
    }
  }
}
