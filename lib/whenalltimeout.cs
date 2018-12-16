using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Distsys.Threading
{
    public static class TaskHelpers
    {
        static public async Task<(bool[], bool[])> WhenAll(IEnumerable<Task> required, int timeoutRequired,
                                                 IEnumerable<Task> optional, int timeoutOptional)
        {
            // Capture the Tasks into an array
            var requiredArray = ConvertToArray(required, "Task in parameter required is null.");
            var optionalArray = ConvertToArray(optional, "Task in parameter optional is null.");

            return await WhenAll(requiredArray, timeoutRequired, optionalArray, timeoutOptional);

#if false
            // Here is a lower level version of the same algoritm - need to compare
            // allocations etc.
            var cts = new CancellationTokenSource();
            var timer = Task.Delay(timeout, cts.Token);

            var taskArray = tasks.ToArray();

            var resultTaskCompletionSource = new TaskCompletionSource<bool[]>();
            var resultArry = new bool[taskArray.Length];
            var remainingTasks = taskArray.Length;

            Action<Task, Object> collector = (antecedent, index) =>
              {
                  resultArry[(int)index] = antecedent.IsCompletedSuccessfully();

                  if (--remainingTasks == 0)
                  {
                      cts.Cancel();
                      cts.Dispose();

                      resultTaskCompletionSource.TrySetResult(resultArry);
                  }
              };

            for (int i = 0; i < taskArray.Length; ++i)
            {
                taskArray[i].ContinueWith(collector, (object)i);
            }

            timer.ContinueWith((antecedent) =>
            {
                cts.Dispose();
                resultTaskCompletionSource.TrySetResult(resultArry);
            });

            return resultTaskCompletionSource.Task;
#endif
        }

        static public async Task<(bool[], bool[])> WhenAll(Task[] required, int timeoutRequired,
                                                 Task[] optional, int timeoutOptional)
        {
            // TODO: This fails to pass exceptions from the tasks back out.

            // TODO: I can't see how to cancel tasks that timeout. I think we would
            // need to create the tasks that way, and then pass the CancelationTokenSource
            // in here.

            bool[] requiredResults = null;
            bool[] optionalResults = null;

            // Start the timer for optional tasks now.
            var taskOptionalTimer = Task.Delay(timeoutOptional);

            // Wait for required tasks (and collect their completion status)
            if (required != null && required.Length > 0)
            {
                await Task.WhenAny(
                    Task.Delay(timeoutRequired),
                    Task.WhenAll(required));
                requiredResults =
                    (from task in required
                     select task.Status == TaskStatus.RanToCompletion).ToArray();
            }

            // Handle optional tasks
            if (optional != null && optional.Length > 0)
            {
                await Task.WhenAny(
                    taskOptionalTimer,
                    Task.WhenAll(optional));
                optionalResults =
                    (from task in optional
                     select task.Status == TaskStatus.RanToCompletion).ToArray();
            }

            return (requiredResults, optionalResults);
        }

        static public async Task<(TResult[], TResult2[])> WhenAll<TResult, TResult2>(
            IEnumerable<Task<TResult>> required, int timeoutRequired,
            IEnumerable<Task<TResult2>> optional, int timeoutOptional)
        {
            // Capture the Tasks into an array
            var requiredArray = ConvertToArray(required, "Task in parameter required is null.");
            var optionalArray = ConvertToArray(optional, "Task in parameter optional is null.");

            return await WhenAll(requiredArray, timeoutRequired, optionalArray, timeoutOptional);
        }

        public static async Task<(TResult[], TResult2[])> WhenAll<TResult, TResult2>(
            Task<TResult>[] required, int timeoutRequired,
            Task<TResult2>[] optional, int timeoutOptional)
        {
            // TODO: This fails to pass exceptions from the tasks back out.

            TResult[] requiredResults = null;
            TResult2[] optionalResults = null;

            // Start the timer for optional tasks now.
            var taskOptionalTimer = Task.Delay(timeoutOptional);

            // Wait for required tasks (and collect their completion status)
            if (required != null && required.Length > 0)
            {
                await Task.WhenAny(
                    Task.Delay(timeoutRequired),
                    Task.WhenAll(required));
                requiredResults =
                    (from task in required
                     select task.Status == TaskStatus.RanToCompletion ?
                        task.Result : default(TResult)).ToArray();
            }

            // Handle optional tasks
            if (optional != null && optional.Length > 0)
            {
                await Task.WhenAny(
                    taskOptionalTimer,
                    Task.WhenAll(optional));
                optionalResults =
                    (from task in optional
                     select task.Status == TaskStatus.RanToCompletion ?
                        task.Result : default(TResult2)).ToArray();
            }

            return (requiredResults, optionalResults);
        }

        static internal T[] ConvertToArray<T>(IEnumerable<T> enumerable, string nullErrorMessage)
        {
            // Convert from an IEnumerable<T> into a T[] as efficiently as we can.
            var array = enumerable as T[];
            if (array != null)
            {
                return array;
            }

            // If we know the size we can avoid several allocations
            ICollection<T> collection = enumerable as ICollection<T>;
            if (collection != null)
            {
                int index = 0;
                array = new T[collection.Count];
                foreach (var item in collection)
                {
                    if (item == null) throw new ArgumentException(nullErrorMessage);
                    array[index++] = item;
                }

                return array;
            }

            if (enumerable == null)
            {
                return null;
            }

            // Fallback
            List<T> list = new List<T>();
            foreach (T item in enumerable)
            {
                if (item == null) throw new ArgumentException(nullErrorMessage);
                list.Add(item);
            }

            return list.ToArray();
        }
    }
}