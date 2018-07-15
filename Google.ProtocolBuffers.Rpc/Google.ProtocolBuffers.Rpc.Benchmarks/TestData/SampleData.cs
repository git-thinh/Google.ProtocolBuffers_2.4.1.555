#region Copyright 2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;
using System.Collections.Generic;

namespace ProtocolBuffers.Rpc.Benchmarks.TestData
{
    struct SampleData
    {
        public byte[] Bytes;
        public string Text;
        public int Number;
        public double Float;
        public DateTime Time;

        public static IEnumerable<T> Generate<T>(int count, Func<SampleData, T> builder)
        {
            Random rand = new Random();
            SampleData value = new SampleData();
            value.Bytes = new byte[32];

            for( int i=0; i < count; i++ )
            {
                rand.NextBytes(value.Bytes);
                value.Text = Convert.ToBase64String(value.Bytes);
                value.Number = i * 1000;
                value.Float = i + (0.0001*i);
                value.Time = DateTime.UtcNow.AddTicks(i);

                yield return builder(value);
            }
        }
    }
}
