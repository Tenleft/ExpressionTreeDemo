using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ToParams
{
    public class Person
    {
        public int this[int i]
        {
            get { return this.Id + i; }
            set { this.Id = value - 1; }
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public bool Gender { get; set; }

        public DateTime BirthDay { get; set; }

        public Car Car { get; set; }

        public string Desc { get; set; }

        public int? Age { get; set; }

    }

    public class Car
    {
        public int Id { get; set; }
        public string CarName { get; set; }
        public override string ToString() => this.CarName;
    }
    public class ObjToUrlParam
    {

        public Person p = new Person() { Age = 18, BirthDay = DateTime.Now, Desc = "I'm Sharp", Gender = true, Id = 1, Name = "Sharp Cham" };
        public Car c = new Car { Id = 2, CarName = "bc" };
        static Dictionary<Type, object> toParamDic = new Dictionary<Type, object>();

        [Benchmark]
        public void ExpressionFuncTest()
        {
            ExpressionFunc(p);
        }
        [Benchmark]
        public void ExpressionFuncTest2()
        {
            ExpressionFunc(c);
        }


        [Benchmark]
        public void ReflectorTest()
        {
            Reflector(p);
        }
        [Benchmark]
        public void ReflectorTest2()
        {
            Reflector(c);
        }

        public string ExpressionFunc<T>(T obj)
        {
            var objType = typeof(T);
            if (obj == null)
            {
                return string.Empty;
            }
            if (objType.IsValueType || objType == typeof(string))
            {
                return Convert.ToString(obj);
            }
            var props = objType.GetProperties();
            if (props.Length == 0)
            {
                return string.Empty;
            }
            if (!toParamDic.ContainsKey(objType))
            {
                lock (toParamDic)
                {
                    var objParam = Expression.Parameter(objType, "obj");
                    var sbType = typeof(StringBuilder);
                    var sbParam = Expression.Parameter(sbType, "sb");
                    var sbCtor = sbType.GetConstructor(new[] { typeof(int) });

                    // 创建 StringBuilder 对象sb => sb = new StringBuilder(64);
                    var sbAssign = Expression.Assign(sbParam, Expression.New(sbCtor, Expression.Constant(64)));

                    var sbStrAppend = sbType.GetMethod("Append", new[] { typeof(string) });// 获取StringBuilder的 Append方法
                    var sbToString = sbType.GetMethod("ToString", Type.EmptyTypes);// 获取StringBuilder的 ToString方法

                    var expList = new List<Expression>();
                    expList.Add(sbAssign);
                    for (int i = 0; i < props.Length; i++)
                    {
                        // 如果是索引则跳过
                        if (props[i].GetIndexParameters()?.Length > 0)
                        {
                            continue;
                        }
                        // sb.Append(props[i].Name)
                        expList.Add(Expression.Call(sbParam, sbStrAppend, Expression.Constant(props[i].Name)));
                        // sb.Append("=")
                        expList.Add(Expression.Call(sbParam, sbStrAppend, Expression.Constant("=")));
                        var pValue = Expression.Property(objParam, props[i].Name);

                        var pValueType = pValue.Type;
                        if (pValueType == typeof(string))// string 类型
                        {
                            expList.Add(Expression.Call(sbParam, sbStrAppend, pValue));
                        }
                        else if (pValueType.IsValueType)// 值类型
                        {
                            var valueToStr = Expression.Call(pValue, pValueType.GetMethod("ToString", Type.EmptyTypes));
                            expList.Add(Expression.Call(sbParam, sbStrAppend, valueToStr));
                        }
                        else // 非string 类型的引用类型
                        {
                            var valueToStr = Expression.Call(pValue, pValueType.GetMethod("ToString", Type.EmptyTypes));
                            var ifElseExp = Expression.IfThenElse(Expression.Equal(pValue, Expression.Constant(null))
                                  , Expression.Call(sbParam, sbStrAppend, Expression.Constant(""))// null值处理  => sb.Append("")
                                  , Expression.Call(sbParam, sbStrAppend, valueToStr)// 非null => sb.Append(value.ToString())
                                  );
                            expList.Add(ifElseExp);
                        }

                        if (props.Length - 1 != i)
                        {
                            // sb.Append("&")
                            expList.Add(Expression.Call(sbParam, sbStrAppend, Expression.Constant("&")));
                        }
                    }
                    // sb.ToString()
                    expList.Add(Expression.Call(sbParam, sbToString));
                    var lambdaExp = Expression.Lambda<Func<T, string>>(
                         Expression.Block(
                             typeof(string)// 返回值类型
                             , new ParameterExpression[] { sbParam }// block块参数
                             , expList.ToArray()// block块内容
                             )
                         , objParam);
                    toParamDic[objType] = lambdaExp.Compile();
                }
            }

            return ((Func<T, string>)toParamDic[objType]).Invoke(obj);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string Reflector<T>(T obj)
        {
            var objType = typeof(T);
            if (obj == null || objType.IsValueType)
            {
                return string.Empty;
            }
            var props = objType.GetProperties();
            StringBuilder sb = new StringBuilder(128);
            for (int i = 0; i < props.Length; i++)
            {
                if (props[i].GetIndexParameters()?.Length > 0)
                {
                    continue;
                }
                sb.Append(props[i].Name);
                sb.Append('=');
                sb.Append(props[i].GetValue(obj));
                if (props.Length - 1 != i)
                {
                    sb.Append('&');
                }
            }
            return sb.ToString();
        }
    }

    class Program
    {

        static void Main(string[] args)
        {
            //MultipleReturn()

            ObjToUrlParam toUrlParam = new ObjToUrlParam();

            Console.WriteLine(toUrlParam.ExpressionFunc("Hello"));
            Console.WriteLine(toUrlParam.ExpressionFunc(1));
            Console.WriteLine(toUrlParam.ExpressionFunc(false));
            Console.WriteLine(toUrlParam.ExpressionFunc(new object()));
            Console.WriteLine(toUrlParam.ExpressionFunc(toUrlParam.c));
            Console.WriteLine(toUrlParam.ExpressionFunc(toUrlParam.p));
            toUrlParam.p.Age = null;
            toUrlParam.p.Car = new Car { CarName = "Baoma" };
            Console.WriteLine(toUrlParam.ExpressionFunc(toUrlParam.p));

            Console.WriteLine(toUrlParam.Reflector(toUrlParam.p));

            //var summary = BenchmarkRunner.Run<ObjToUrlParam>();

            Console.ReadKey();
        }

        /// <summary>
        /// 多分支带返回值案例
        /// </summary>
        static void MultipleReturn()
        {
            /*
                if (n < 2)
                {
                    return 1;
                }
                Console.WriteLine("Hello");
                Console.WriteLine("World");
                Console.WriteLine("~~~~~~");
                if (n < 4)
                {
                    return 2;
                }
                Console.WriteLine("end.");
                return 10;
             */
            LabelTarget return1 = Expression.Label(typeof(int), "return1");

            var returnExp = Expression.Label(return1, Expression.Constant(10));

            var numExp = Expression.Parameter(typeof(int), "num");
            var cw = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
            var call1 = Expression.Call(cw, Expression.Constant("Hello", typeof(string)));
            var call2 = Expression.Call(cw, Expression.Constant("World", typeof(string)));
            var call3 = Expression.Call(cw, Expression.Constant("~~~~~~", typeof(string)));
            var call4 = Expression.Call(cw, Expression.Constant("end.", typeof(string)));

            var conditional1 = Expression.IfThen(
               Expression.LessThan(numExp, Expression.Constant(2))
               , Expression.Return(return1, Expression.Constant(1))
               );
            var conditional2 = Expression.IfThen(
                Expression.LessThan(numExp, Expression.Constant(4))
                , Expression.Return(return1, Expression.Constant(2)));

            var lambdaExp = Expression.Lambda<Func<int, int>>(
                Expression.Block(typeof(int),
                  new ParameterExpression[] { }
                , new Expression[] { conditional1,call1,call2,call3,conditional2,call4, returnExp,
                 })
                , numExp);
            var func = lambdaExp.Compile();
            Console.WriteLine(func(1));
            Console.WriteLine("-".PadLeft(20, '-'));
            Console.WriteLine(func(3));
            Console.WriteLine("-".PadLeft(20, '-'));
            Console.WriteLine(func(7));
        }
    }
}
