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
using System.Collections.Generic;
using System.Text;
using System.IO;
using Behaviac.Design;
using Behaviac.Design.Nodes;
using PluginBehaviac.Nodes;
using PluginBehaviac.DataExporters;

namespace PluginBehaviac.NodeExporters
{
    public class DecoratorIteratorCsExporter : DecoratorCsExporter
    {
        protected override bool ShouldGenerateClass(Node node)
        {
            DecoratorIterator iterator = node as DecoratorIterator;
            return (iterator != null);
        }

        protected override void GenerateConstructor(Node node, StreamWriter stream, string indent, string className)
        {
            base.GenerateConstructor(node, stream, indent, className);

            DecoratorIterator iterator = node as DecoratorIterator;
            if (iterator == null)
                return;

            if (iterator.Opl != null || iterator.Opr != null)
            {
                if (iterator.Opl != null)
                {
                    stream.WriteLine("{0}\t\t\tthis.m_opl = AgentMeta.ParseProperty(\"{1}\");", indent, iterator.Opl.GetExportValue());
                    stream.WriteLine("{0}\t\t\tDebug.Check(this.m_opl != null);", indent);
                }

                if (iterator.Opr != null)
                {
                    stream.WriteLine("{0}\t\t\tthis.m_opr = AgentMeta.ParseProperty(\"{1}\");", indent, iterator.Opr.GetExportValue());
                    stream.WriteLine("{0}\t\t\tDebug.Check(this.m_opr != null);", indent);
                }
            }
        }
    }
}
