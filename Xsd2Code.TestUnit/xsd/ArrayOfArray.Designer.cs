// ------------------------------------------------------------------------------
//  <auto-generated>
//    Generated by Xsd2Code. Version 2.8.2.29599
//    <NameSpace>Xsd2Code.TestUnit</NameSpace><Collection>Array</Collection><codeType>CSharp</codeType><EnableDataBinding>False</EnableDataBinding><HidePrivateFieldInIDE>False</HidePrivateFieldInIDE><EnableSummaryComment>False</EnableSummaryComment><IncludeSerializeMethod>False</IncludeSerializeMethod><GenerateCloneMethod>False</GenerateCloneMethod><GenerateDataContracts>False</GenerateDataContracts><CodeBaseTag>Net20</CodeBaseTag><SerializeMethodName>Serialize</SerializeMethodName><DeserializeMethodName>Deserialize</DeserializeMethodName><SaveToFileMethodName>SaveToFile</SaveToFileMethodName><LoadFromFileMethodName>LoadFromFile</LoadFromFileMethodName><GenerateXMLAttributes>False</GenerateXMLAttributes><AutomaticProperties>False</AutomaticProperties><DisableDebug>False</DisableDebug><CustomUsings></CustomUsings>
//  <auto-generated>
// ------------------------------------------------------------------------------
namespace Xsd2Code.TestUnit
{
    using System;
    using System.Diagnostics;
    using System.Xml.Serialization;
    using System.Collections;
    using System.Xml.Schema;
    using System.ComponentModel;


    public partial class root
    {

        private string[][] elementField;

        [System.Xml.Serialization.XmlArrayItemAttribute("FirstName", typeof(string), IsNullable = false)]
        public string[][] Element
        {
            get
            {
                return this.elementField;
            }
            set
            {
                this.elementField = value;
            }
        }
    }
}
