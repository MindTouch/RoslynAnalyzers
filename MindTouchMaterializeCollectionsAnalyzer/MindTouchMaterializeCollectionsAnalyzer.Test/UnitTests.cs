﻿/**
 * Copyright (c) 2016 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace MindTouchMaterializeCollectionsAnalyzer.Test {

    [TestClass]
    public class UnitTest : CodeFixVerifier {
        
        [TestMethod]
        public void Correct_materialized_code() {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public void Add(IEnumerable<int> a, IEnumerable<int> b) {
                return;
            }

            public void Test() {
                var l1 = new List<int>();
                var l2 = new List<int>();
                Add(l1, l2);
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }
        
        [TestMethod]
        public void Select_statement_is_detected_unmaterialized_collection() {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public void Add(IEnumerable<int> a, IEnumerable<int> b) {
                return;
            }

            public void Test() {
                var l1 = new List<int>();
                var l2 = new List<int>();
                Add(l1, l2.Select(x => x+x));
            }
        }
    }";
            var expected = new DiagnosticResult {
                Id = "MaterializeCollectionsAnalyzer",
                Message = "Collection should be materialized to a specific type",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 18, 25)
                        }
            };
            VerifyCSharpDiagnostic(test, expected);
            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public void Add(IEnumerable<int> a, IEnumerable<int> b) {
                return;
            }

            public void Test() {
                var l1 = new List<int>();
                var l2 = new List<int>();
                Add(l1, l2.Select(x => x+x).ToArray());
            }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void Select_statement_is_detected_unmaterialized_collection_when_not_directly_in_argument() {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public void Add(IEnumerable<int> a, IEnumerable<int> b) {
                return;
            }

            public void Test() {
                var l1 = new List<int>();
                var l2 = new List<int>().Select(x => x+x);
                Add(l1, l2);
            }
        }
    }";
            var expected = new DiagnosticResult {
                Id = "MaterializeCollectionsAnalyzer",
                Message = "Collection should be materialized to a specific type",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 18, 25)
                        }
            };
            VerifyCSharpDiagnostic(test, expected);
            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public void Add(IEnumerable<int> a, IEnumerable<int> b) {
                return;
            }

            public void Test() {
                var l1 = new List<int>();
                var l2 = new List<int>().Select(x => x+x);
                Add(l1, l2.ToArray());
            }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void Unmaterialized_collections_in_return_statements_are_flagged() {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public IEnumerable<int> Add(IEnumerable<int> a, IEnumerable<int> b) {
                var l1 = new List<int>().Select(x => x+1);
                return l1;
            }
        }
    }";
            var expected = new DiagnosticResult {
                Id = "MaterializeCollectionsAnalyzer",
                Message = "Collection should be materialized to a specific type",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 13, 24)
                        }
            };
            VerifyCSharpDiagnostic(test, expected);
            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public IEnumerable<int> Add(IEnumerable<int> a, IEnumerable<int> b) {
                var l1 = new List<int>().Select(x => x+1);
                return l1.ToArray();
            }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void Values_coming_straight_from_method_call_directly_are_assumed_materialized() {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public static IEnumerable<int> GetCollection() {
                return new List<int>();
            }
            public IEnumerable<int> Add(IEnumerable<int> a, IEnumerable<int> b) {
                var l1 = new List<int>().Select(x => x+1).ToArray();
                return l1;
            }
            public static void Main() {
                var x = Add(GetCollection(), GetCollection());
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Values_coming_straight_from_method_call_directly_are_assumed_materialized_in_return_statement() {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public static IEnumerable<int> GetCollection() {
                return new List<int>();
            }
            public IEnumerable<int> Add(IEnumerable<int> a, IEnumerable<int> b) {
                var l1 = new List<int>().Select(x => x+1).ToArray();
                return l1;
            }
            public static IEnumerable<int> Main() {
                return TypeName.Add(GetCollection(), GetCollection());
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Values_coming_straight_from_method_call_indirectly_are_assumed_materialized() {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public static IEnumerable<int> GetCollection() {
                return new List<int>();
            }
            public IEnumerable<int> Add(IEnumerable<int> a, IEnumerable<int> b) {
                var l1 = new List<int>().Select(x => x+1).ToArray();
                return l1;
            }
            public static void Main() {
                var a = GetCollection();
                var b = GetCollection();
                var x = Add(a, b);
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Values_of_ienumerable_type_with_subsequent_materialization() {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public static IEnumerable<int> GetCollection() {
                return new List<int>();
            }
            public IEnumerable<int> Add(IEnumerable<int> a, IEnumerable<int> b) {
                var l1 = new List<int>().Select(x => x+1).ToArray();
                return l1;
            }
            public static void Main() {
                IEnumerable<int> a;
                IEnumerable<int> b;
                a = GetCollection();
                b = GetCollection();
                var x = Add(a, b);
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Values_coming_straight_from_property_call_directly_are_assumed_materialized_in_return_statement() {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public IEnumerable<int> MyCollectionProperty {
                get { return new List<int>(); }
            }
            public IEnumerable<int> Add(IEnumerable<int> a, IEnumerable<int> b) {
                var l1 = new List<int>().Select(x => x+1).ToArray();
                return l1;
            }
            public static IEnumerable<int> Main() {
                var t = new TypeName();
                return Add(t.MyCollectionProperty, t.MyCollectionProperty);
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Values_coming_straight_from_property_call_indirectly_are_assumed_materialized() {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public IEnumerable<int> MyCollectionProperty {
                get { return new List<int>(); }
            }
            public IEnumerable<int> Add(IEnumerable<int> a, IEnumerable<int> b) {
                var l1 = new List<int>().Select(x => x+1).ToArray();
                return l1;
            }
            public static void Main() {
                var t = new TypeName();
                var a = t.MyCollectionProperty;
                var b = t.MyCollectionProperty;
                var x = Add(a, b);
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Values_coming_straight_from_method_params_directly_are_assumed_materialized() {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public IEnumerable<int> Add(IEnumerable<int> a, IEnumerable<int> b) {
                var l1 = new List<int>().Select(x => x+1).ToArray();
                return l1;
            }
            public static void Main(IEnumerable<int> a, IEnumerable<int> b) {
                var x = Add(a, b);
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Values_coming_straight_from_method_params_indirectly_are_assumed_materialized() {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1 {
        class TypeName {
            public IEnumerable<int> Add(IEnumerable<int> a, IEnumerable<int> b) {
                var l1 = new List<int>().Select(x => x+1).ToArray();
                return l1;
            }
            public static void Main(IEnumerable<int> a, IEnumerable<int> b) {
                var i = a;
                var j = b;
                var x = Add(i, j);
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider() {
            return new MaterializeCollectionsAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
            return new MaterializeCollectionsAnalyzerAnalyzer();
        }
    }
}