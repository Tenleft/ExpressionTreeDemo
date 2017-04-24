using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExpressionTreeExample1
{
    class Program
    {
        static void Main(string[] args)
        {
            //http://www.cnblogs.com/Ninputer/archive/2009/08/28/expression_tree1.html
            Test10();
            Console.ReadKey();
        }

        #region 练习一：-a

        public static void Test1()
        {
            //ParameterExpression paramExp = Expression.Parameter(typeof(int), "a");
            //BinaryExpression binaryExp = Expression.Subtract(Expression.Constant(0), paramExp);
            //Expression<Func<int, int>> exp = Expression.Lambda<Func<int, int>>(binaryExp, paramExp);

            ParameterExpression paramExp2 = Expression.Parameter(typeof(int), "a");

            var exp = Expression.Lambda<Func<int, int>>(Expression.Negate(paramExp2), paramExp2);


            var result = exp.Compile()(10);
            Console.WriteLine(result);
        }

        #endregion

        #region 练习二 a+b*2

        public static void Test2()
        {
            ParameterExpression aParam = Expression.Parameter(typeof(int), "a");
            ParameterExpression bParam = Expression.Parameter(typeof(int), "b");

            BinaryExpression rightExp = Expression.Multiply(bParam, Expression.Constant(2));

            BinaryExpression result = Expression.Add(aParam, rightExp);

            var lambda = Expression.Lambda<Func<int, int, int>>(result, aParam, bParam);
            Console.WriteLine(lambda.Compile()(2, 3));
        }
        #endregion

        #region 练习三 Math.Sin(x) + Math.Cos(y)

        public static void Test3()
        {
            ParameterExpression xParam = Expression.Parameter(typeof(double), "x");
            ParameterExpression yParam = Expression.Parameter(typeof(double), "y");

            MethodCallExpression left = Expression.Call(typeof(Math).GetMethod("Sin", new[] { typeof(double) }), xParam);

            MethodCallExpression right = Expression.Call(typeof(Math).GetMethod("Cos", new[] { typeof(double) }), yParam);

            var lambda = Expression.Lambda<Func<double, double, double>>(Expression.Add(left, right), xParam, yParam);
            Console.WriteLine(lambda.Compile()(2, 3));
        }
        #endregion

        #region 练习四 new StringBuilder(“Hello”)

        public static void Test4()
        {
            ParameterExpression xParam = Expression.Parameter(typeof(string), "x");
            ConstructorInfo ctor = typeof(StringBuilder).GetConstructor(new[] { typeof(string) });
            NewExpression newExp = Expression.New(ctor, xParam);
            var lambda = Expression.Lambda<Func<string, int>>(Expression.Property(newExp, "Length"), xParam);
            Console.WriteLine(lambda.Compile()("Stephen"));
        }
        #endregion

        #region 练习五 new int[] { a, b, a + b}

        public static void Test5()
        {
            ParameterExpression aParam = Expression.Parameter(typeof(int), "a");
            ParameterExpression bParam = Expression.Parameter(typeof(int), "b");
            NewArrayExpression newArr = Expression.NewArrayInit(typeof(int), aParam, bParam, Expression.Add(aParam, bParam));

            ParameterExpression index = Expression.Parameter(typeof(int), "index");

            BinaryExpression binaryExp = Expression.ArrayIndex(newArr, index);

            var lambda = Expression.Lambda<Func<int, int, int, int>>(binaryExp, aParam, bParam, index);

            Console.WriteLine(lambda.Compile()(2, 3, 2));
        }
        #endregion

        #region 练习六 a[i – 1] * i

        public static void Test6()
        {
            ParameterExpression iParam = Expression.Parameter(typeof(int), "i");

            ParameterExpression sParam = Expression.Parameter(typeof(string), "s");

            IndexExpression indexExp = Expression.Property(sParam, typeof(string).GetProperty("Chars", new[] { typeof(int) }), Expression.Subtract(iParam, Expression.Constant(1)));

            UnaryExpression convert = Expression.Convert(indexExp, typeof(int));

            var lambda = Expression.Lambda<Func<string, int, int>>(Expression.Multiply(convert, iParam), sParam, iParam);

            char t = 't';// 116

            Console.WriteLine(lambda.Compile()("Stephen", 2));
        }
        #endregion

        #region 练习七 a.Length > b | b >= 0

        public static void Test7()
        {

            ParameterExpression aParam = Expression.Parameter(typeof(string), "a");

            ParameterExpression bParam = Expression.Parameter(typeof(int), "b");

            BinaryExpression left = Expression.GreaterThan(Expression.Property(aParam, "Length"), bParam);

            BinaryExpression right = Expression.GreaterThanOrEqual(bParam, Expression.Constant(0));

            //or 运算：1|1= 1 , 1|0= 1 , 0|0= 0 , 0|1= 1.
            var lambda = Expression.Lambda<Func<string, int, bool>>(Expression.Or(left, right), aParam, bParam);

            Console.WriteLine(lambda.Compile()("Stephen", 2));
        }
        #endregion

        #region 练习八 new Point() { X = Math.Sin(a), Y = Math.Cos(a) }

        public static void Test8()
        {
            var pType = typeof(Point);

            ParameterExpression aParam = Expression.Parameter(typeof(double), "a");
            ParameterExpression pointParam = Expression.Parameter(pType, "point");

            MethodInfo sinInfo = typeof(Math).GetMethod("Sin", new[] { typeof(double) });
            MethodInfo cosInfo = typeof(Math).GetMethod("Cos", new[] { typeof(double) });

            MemberInitExpression memberInit = Expression.MemberInit(Expression.New(pType),
                     Expression.Bind(pType.GetProperty("X", BindingFlags.Public | BindingFlags.Instance)
                         , Expression.Call(sinInfo, aParam))
                     , Expression.Bind(pType.GetProperty("Y", BindingFlags.Public | BindingFlags.Instance)
                         , Expression.Call(cosInfo, aParam))
                         );
            BinaryExpression binaryExp = Expression.Assign(pointParam, memberInit);

            var lambda = Expression.Lambda<Func<double, double>>(Expression.Block(new[] { pointParam }, binaryExp
                  , Expression.Add(Expression.Property(pointParam, "X"), Expression.Property(pointParam, "Y")))
                  , aParam);

            double c = Math.Sin(2) + Math.Cos(2);//0.4931505902785393

            Console.WriteLine(lambda.Compile()(2));//0.493150590278539
        }
        #endregion

        #region 练习九 循环10次输出“努力，奋斗”

        public static void Test9()
        {

            ParameterExpression pName = Expression.Parameter(typeof(string), "name");
            ParameterExpression pIndex = Expression.Parameter(typeof(int), "index");
            LabelTarget ltBreak = Expression.Label();
            //输出参数数组
            Expression[] concatArgs = new Expression[] { pName, Expression.Constant(":\t努力，奋斗\t"), Expression.Call(pIndex, "Tostring", null) };

            //连接输出参数.注意不能使用：Expression.Add。字符串的拼接最后就是调用的string.Concat()方法
            MethodCallExpression concatExp = Expression.Call(
                typeof(string).GetMethod("Concat", Enumerable.Repeat(typeof(string), 3).ToArray())
                , concatArgs);
            //调用Console.WriteLine()输出。
            MethodCallExpression writeExp = Expression.Call(typeof(Console).GetMethod("WriteLine", new[] { typeof(string) }), concatExp);

            //创建循环
            BlockExpression blockExp = Expression.Block(
                new[] { pIndex }
                , Expression.Assign(pIndex, Expression.Constant(0))
                , Expression.Loop(
                       Expression.IfThenElse(Expression.LessThan(pIndex, Expression.Constant(10))//index<10
                       , Expression.Block(Expression.PostIncrementAssign(pIndex), writeExp)//index++;Console.WriteLine(concatExp)
                       , Expression.Break(ltBreak)//跳出循环
                       )
                   , ltBreak)
               );

            var lambda = Expression.Lambda<Action<string>>(blockExp, pName);

            lambda.Compile()("张威");

        }
        #endregion

        #region 练习十 快速排序

        public static void Test10()
        {

            ParameterExpression pArr = Expression.Parameter(typeof(int[]), "arr");
            ParameterExpression pStart = Expression.Parameter(typeof(int), "start");
            ParameterExpression pEnd = Expression.Parameter(typeof(int), "end");

            ParameterExpression pQuickSort = Expression.Parameter(typeof(Action<int[], int, int>), "QuickSort");
            ParameterExpression s = Expression.Parameter(typeof(int), "s");
            ParameterExpression e = Expression.Parameter(typeof(int), "e");
            ParameterExpression temp = Expression.Parameter(typeof(int), "temp");

            LabelTarget ltBreak1 = Expression.Label();
            LabelTarget ltBreak2 = Expression.Label();
            LabelTarget ltBreak3 = Expression.Label();

            MethodInfo arrSet = typeof(int[]).GetMethod("Set", new[] { typeof(int), typeof(int) });

            BinaryExpression assign = Expression.Assign(pQuickSort, Expression.Lambda<Action<int[], int, int>>(
                Expression.Block(
                    new[] { s, e, temp }
                    , Expression.Assign(s, pStart)
                    , Expression.Assign(e, pEnd)
                    , Expression.Assign(temp, Expression.ArrayIndex(pArr, e))
                    , Expression.IfThen(Expression.LessThan(s, e),
                        Expression.Block(
                        Expression.Loop(
                            Expression.IfThenElse(Expression.LessThan(s, e)
                            , Expression.Block(
                                Expression.Loop(
                                    Expression.IfThenElse(Expression.AndAlso(Expression.LessThan(s, e)
                                    , Expression.LessThanOrEqual(Expression.ArrayIndex(pArr, s), temp))
                                    , Expression.PostIncrementAssign(s)
                                    , Expression.Break(ltBreak1)), ltBreak1
                                )
                                , Expression.Call(pArr, arrSet, e, Expression.ArrayIndex(pArr, s))

                                , Expression.Loop(
                                    Expression.IfThenElse(Expression.AndAlso(Expression.LessThan(s, e)
                                    , Expression.GreaterThanOrEqual(Expression.ArrayIndex(pArr, e), temp))
                                    , Expression.PostDecrementAssign(e)
                                    , Expression.Break(ltBreak2)), ltBreak2
                                    )
                                , Expression.Call(pArr, arrSet, s, Expression.ArrayIndex(pArr, e))
                            )
                            , Expression.Break(ltBreak3))
                        , ltBreak3)
                        , Expression.Call(pArr, arrSet, s, temp)
                        , Expression.Invoke(pQuickSort, pArr, pStart, Expression.Decrement(s))
                        , Expression.Invoke(pQuickSort, pArr, Expression.Increment(s), pEnd)
                     )
                   )
                ), pArr, pStart, pEnd));

            BlockExpression block = Expression.Block(new[] { pQuickSort }, assign, Expression.Invoke(pQuickSort, pArr, pStart, pEnd));
            int[] arr = { 1, 3, 42, 5, 33, 22, 2, 39, 5, 37, 21 };
            int[] arr2 = new int[arr.Length];
            arr.CopyTo(arr2, 0);
            var lambda = Expression.Lambda<Action<int[], int, int>>(block, pArr, pStart, pEnd);

            lambda.Compile()(arr, 0, arr.Length - 1);
            QuickSort(arr2, 0, arr2.Length - 1);
            Console.WriteLine("arr :" + string.Join(",", arr));//1,2,3,5,5,21,22,33,37,39,42
            Console.WriteLine("arr2:" + string.Join(",", arr2));//1,2,3,5,5,21,22,33,37,39,42

        }

        /// <summary>
        /// 习题十所以生成的代码与此方法类似
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        static void QuickSort(int[] arr, int start, int end)
        {
            int s = start, e = end, temp = arr[e];
            if (s < e)
            {
                while (s < e)
                {
                    while (s < e && arr[s] <= temp)
                    {
                        s++;
                    }
                    arr[e] = arr[s];
                    while (s < e && arr[e] >= temp)
                    {
                        e--;
                    }
                    arr[s] = arr[e];
                }
                arr[s] = temp;
                QuickSort(arr, start, s - 1);
                QuickSort(arr, s + 1, end);
            }
        }


        #endregion

    }
    struct Point
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}
