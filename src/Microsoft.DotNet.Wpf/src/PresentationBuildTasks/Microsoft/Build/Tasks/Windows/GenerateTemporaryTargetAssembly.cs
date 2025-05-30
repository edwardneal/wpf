﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//---------------------------------------------------------------------------
//
// Description: This is a MSBuild task which generates a temporary target assembly
//              if current project contains a xaml file with local-type-reference.
//
//              It generates a temporary project file and then call build-engine
//              to compile it.
//
//              The new project file will replace all the Reference Items with the
//              resolved ReferencePath, add all the generated code file into Compile
//              Item list.
//
//---------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Xml;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using MS.Utility;

namespace Microsoft.Build.Tasks.Windows
{
    #region GenerateTemporaryTargetAssembly Task class

    /// <summary>
    ///   This task is used to generate a temporary target assembly. It generates
    ///   a temporary project file and then compile it.
    ///
    ///   The generated project file is based on current project file, with below
    ///   modification:
    ///
    ///       A:  Add the generated code files (.g.cs) to Compile Item list.
    ///       B:  Replace Reference Item list with ReferenctPath item list.
    ///           So that it doesn't need to rerun time-consuming task
    ///           ResolveAssemblyReference (RAR) again.
    ///
    /// </summary>
    public sealed class GenerateTemporaryTargetAssembly : Task
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public GenerateTemporaryTargetAssembly()
            : base(SR.SharedResourceManager)
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// ITask Execute method
        /// </summary>
        /// <returns></returns>
        /// <remarks>Catching all exceptions in this method is appropriate - it will allow the build process to resume if possible after logging errors</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public override bool Execute()
        {
            if (!string.Equals(IncludePackageReferencesDuringMarkupCompilation, "false", StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteGenerateTemporaryTargetAssemblyWithPackageReferenceSupport();
            }
            else
            {
                return ExecuteLegacyGenerateTemporaryTargetAssembly();
            }
        }

        /// <summary>
        /// ExecuteLegacyGenerateTemporaryTargetAssembly
        ///
        /// Creates a project file based on the parent project and compiles a temporary assembly.
        ///
        /// Passes IntermediateOutputPath, AssemblyName, and TemporaryTargetAssemblyName as global properties.
        ///
        /// </summary>
        /// <returns></returns>
        /// <remarks>Catching all exceptions in this method is appropriate - it will allow the build process to resume if possible after logging errors</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool ExecuteLegacyGenerateTemporaryTargetAssembly()
        {
            bool retValue = true;

            // Verification
            try
            {
                XmlDocument xmlProjectDoc = null;

                xmlProjectDoc = new XmlDocument( );
                //Bugfix for GB chars, exception thrown when using Load(CurrentProject), when project name has GB characters in it.
                //Using a filestream instead of using string path to avoid the need to properly compose Uri (which is another way of fixing - but more complicated).
                using(FileStream fs = File.OpenRead(CurrentProject))
                {
                    xmlProjectDoc.Load(fs);
                }
                //
                // remove all the WinFX specific item lists
                // ApplicationDefinition, Page, MarkupResource and Resource
                //

                RemoveItemsByName(xmlProjectDoc, APPDEFNAME);
                RemoveItemsByName(xmlProjectDoc, PAGENAME);
                RemoveItemsByName(xmlProjectDoc, MARKUPRESOURCENAME);
                RemoveItemsByName(xmlProjectDoc, RESOURCENAME);

                // Replace the Reference Item list with ReferencePath

                RemoveItemsByName(xmlProjectDoc, REFERENCETYPENAME);
                AddNewItems(xmlProjectDoc, ReferencePathTypeName, ReferencePath);

                // Add GeneratedCodeFiles to Compile item list.
                AddNewItems(xmlProjectDoc, CompileTypeName, GeneratedCodeFiles);

                string currentProjectName = Path.GetFileNameWithoutExtension(CurrentProject);
                string currentProjectExtension = Path.GetExtension(CurrentProject);

                // Create a random file name
                // This can fix the problem of project cache in VS.NET environment.
                //
                // GetRandomFileName( ) could return any possible file name and extension
                // Since this temporary file will be used to represent an MSBUILD project file,
                // we will use the same extension as that of the current project file
                //
                string randomFileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

                // Don't call Path.ChangeExtension to append currentProjectExtension. It will do
                // odd things with project names that already contains a period (like System.Windows.
                // Contols.Ribbon.csproj). Instead, just append the extension - after all, we already know
                // for a fact that this name (i.e., tempProj) lacks a file extension.
                string tempProjPrefix = string.Join("_", currentProjectName, randomFileName, WPFTMP);
                string tempProj = tempProjPrefix  + currentProjectExtension;


                // Save the xmlDocument content into the temporary project file.
                xmlProjectDoc.Save(tempProj);

                //
                // Invoke MSBUILD engine to build this temporary project file.
                //

                Hashtable globalProperties = new Hashtable(3);

                // Add AssemblyName, IntermediateOutputPath and _TargetAssemblyProjectName to the global property list
                // Note that _TargetAssemblyProjectName is not defined as a property with Output attribute - that doesn't do us much
                // good here. We need _TargetAssemblyProjectName to be a well-known property in the new (temporary) project
                // file, and having it be available in the current MSBUILD process is not useful.
                globalProperties[intermediateOutputPathPropertyName] = IntermediateOutputPath;

                globalProperties[assemblyNamePropertyName] = AssemblyName;
                globalProperties[targetAssemblyProjectNamePropertyName] = currentProjectName;
                globalProperties["EmbedUntrackedSources"] = "false";
                Dictionary<string, ITaskItem[]> targetOutputs = new Dictionary<string, ITaskItem[]>();
                retValue = BuildEngine.BuildProjectFile(tempProj, new string[] { CompileTargetName }, globalProperties, targetOutputs);

                // If the inner build succeeds, retrieve the path to the local type assembly from the task's TargetOutputs.
                if (retValue)
                {
                    // See Microsoft.WinFX.targets: TargetOutputs from '_CompileTemporaryAssembly' will always contain one item.
                    // <Target Name="_CompileTemporaryAssembly"  DependsOnTargets="$(_CompileTemporaryAssemblyDependsOn)" Returns="$(IntermediateOutputPath)$(TargetFileName)"/>
                    Debug.Assert(targetOutputs.ContainsKey(CompileTargetName));
                    Debug.Assert(targetOutputs[CompileTargetName].Length == 1);
                    TemporaryAssemblyForLocalTypeReference = targetOutputs[CompileTargetName][0].ItemSpec;
                }

                // Delete the temporary project file and generated files unless diagnostic mode has been requested
                if (!GenerateTemporaryTargetAssemblyDebuggingInformation)
                {
                    try
                    {
                        File.Delete(tempProj);

                        DirectoryInfo intermediateOutputPath = new DirectoryInfo(IntermediateOutputPath);
                        foreach (FileInfo temporaryProjectFile in intermediateOutputPath.EnumerateFiles($"{tempProjPrefix}*"))
                        {
                            temporaryProjectFile.Delete();
                        }
                    }
                    catch (IOException e)
                    {
                        // Failure to delete the file is a non fatal error
                        Log.LogWarningFromException(e);
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                retValue = false;
            }

            return retValue;
        }

        /// <summary>
        /// ExecuteGenerateTemporaryTargetAssemblyWithPackageReferenceSupport
        ///
        /// Creates a project file based on the parent project and compiles a temporary assembly.
        ///
        /// Receives the temporary project name as a parameter and writes properties in to the project file itself.
        ///
        /// No global properties are set.
        ///
        /// </summary>
        /// <returns></returns>
        /// <remarks>Catching all exceptions in this method is appropriate - it will allow the build process to resume if possible after logging errors</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool ExecuteGenerateTemporaryTargetAssemblyWithPackageReferenceSupport()
        {
            bool retValue = true;

            //
            // Create the temporary target assembly project
            //
            try
            {
                XmlDocument xmlProjectDoc = null;

                xmlProjectDoc = new XmlDocument( );
                //Bugfix for GB chars, exception thrown when using Load(CurrentProject), when project name has GB characters in it.
                //Using a filestream instead of using string path to avoid the need to properly compose Uri (which is another way of fixing - but more complicated).
                using(FileStream fs = File.OpenRead(CurrentProject))
                {
                    xmlProjectDoc.Load(fs);
                }
                // remove all the WinFX specific item lists
                // ApplicationDefinition, Page, MarkupResource and Resource
                RemoveItemsByName(xmlProjectDoc, APPDEFNAME);
                RemoveItemsByName(xmlProjectDoc, PAGENAME);
                RemoveItemsByName(xmlProjectDoc, MARKUPRESOURCENAME);
                RemoveItemsByName(xmlProjectDoc, RESOURCENAME);

                // Replace the Reference Item list with ReferencePath
                RemoveItemsByName(xmlProjectDoc, REFERENCETYPENAME);
                AddNewItems(xmlProjectDoc, ReferencePathTypeName, ReferencePath);

                // Add GeneratedCodeFiles to Compile item list.
                AddNewItems(xmlProjectDoc, CompileTypeName, GeneratedCodeFiles);

                // Add Analyzers to Analyzer item list.
                AddNewItems(xmlProjectDoc, AnalyzerTypeName, Analyzers);

                // Replace implicit SDK imports with explicit SDK imports
                ReplaceImplicitImports(xmlProjectDoc);

                // Add properties required for temporary assembly compilation
                var properties = new List<(string PropertyName, string PropertyValue)>
                {
                    ( nameof(AssemblyName), AssemblyName ),
                    ( nameof(IntermediateOutputPath), IntermediateOutputPath ),
                    ( nameof(BaseIntermediateOutputPath), BaseIntermediateOutputPath ),
                    ( nameof(MSBuildProjectExtensionsPath), MSBuildProjectExtensionsPath ),
                    ( "_TargetAssemblyProjectName", Path.GetFileNameWithoutExtension(CurrentProject) ),
                    ( nameof(RootNamespace), RootNamespace ),
                };

                //Removing duplicate AssemblyName
                RemovePropertiesByName(xmlProjectDoc, nameof(AssemblyName));

                AddNewProperties(xmlProjectDoc, properties);

                // Save the xmlDocument content into the temporary project file.
                xmlProjectDoc.Save(TemporaryTargetAssemblyProjectName);

                //
                //  Compile the temporary target assembly project
                //
                Dictionary<string, ITaskItem[]> targetOutputs = new Dictionary<string, ITaskItem[]>();
                retValue = BuildEngine.BuildProjectFile(TemporaryTargetAssemblyProjectName, new string[] { CompileTargetName }, null, targetOutputs);

                // If the inner build succeeds, retrieve the path to the local type assembly from the task's TargetOutputs.
                if (retValue)
                {
                    // See Microsoft.WinFX.targets: TargetOutputs from '_CompileTemporaryAssembly' will always contain one item.
                    // <Target Name="_CompileTemporaryAssembly"  DependsOnTargets="$(_CompileTemporaryAssemblyDependsOn)" Returns="$(IntermediateOutputPath)$(TargetFileName)"/>
                    Debug.Assert(targetOutputs.ContainsKey(CompileTargetName));
                    Debug.Assert(targetOutputs[CompileTargetName].Length == 1);
                    TemporaryAssemblyForLocalTypeReference = targetOutputs[CompileTargetName][0].ItemSpec;
                }

                // Delete the temporary project file and generated files unless diagnostic mode has been requested
                if (!GenerateTemporaryTargetAssemblyDebuggingInformation)
                {
                    try
                    {
                        File.Delete(TemporaryTargetAssemblyProjectName);

                        DirectoryInfo intermediateOutputPath = new DirectoryInfo(IntermediateOutputPath);
                        foreach (FileInfo temporaryProjectFile in intermediateOutputPath.EnumerateFiles($"{Path.GetFileNameWithoutExtension(TemporaryTargetAssemblyProjectName)}*"))
                        {
                            temporaryProjectFile.Delete();
                        }
                    }
                    catch (IOException e)
                    {
                        // Failure to delete the file is a non fatal error
                        Log.LogWarningFromException(e);
                    }
                }

            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                retValue = false;
            }

            return retValue;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// CurrentProject
        ///    The full path of current project file.
        /// </summary>
        [Required]
        public string  CurrentProject
        {
            get { return _currentProject; }
            set { _currentProject = value; }
        }

        /// <summary>
        /// MSBuild Binary Path.
        ///   This is required for Project to work correctly.
        /// </summary>
        [Required]
        public string MSBuildBinPath
        {
            get { return _msbuildBinPath; }
            set { _msbuildBinPath = value; }
        }

        /// <summary>
        /// GeneratedCodeFiles
        ///    A list of generated code files, it could be empty.
        ///    The list will be added to the Compile item list in new generated project file.
        /// </summary>
        public ITaskItem[] GeneratedCodeFiles
        {
            get { return _generatedCodeFiles; }
            set { _generatedCodeFiles = value; }
        }

        /// <summary>
        /// CompileTypeName
        ///   The appropriate item name which can be accepted by managed compiler task.
        ///   It is "Compile" for now.
        ///
        ///   Adding this property is to make the type name configurable, if it is changed,
        ///   No code is required to change in this task, but set a new type name in project file.
        /// </summary>
        [Required]
        public string CompileTypeName
        {
            get { return _compileTypeName; }
            set { _compileTypeName = value; }
        }


        /// <summary>
        /// ReferencePath
        ///    A list of resolved reference assemblies.
        ///    The list will replace the original Reference item list in generated project file.
        /// </summary>
        public ITaskItem[] ReferencePath
        {
            get { return _referencePath; }
            set { _referencePath = value; }
        }

        /// <summary>
        /// ReferencePathTypeName
        ///   The appropriate item name which is used to keep the Reference list in managed compiler task.
        ///   It is "ReferencePath" for now.
        ///
        ///   Adding this property is to make the type name configurable, if it is changed,
        ///   No code is required to change in this task, but set a new type name in project file.
        /// </summary>
        [Required]
        public string ReferencePathTypeName
        {
            get { return _referencePathTypeName; }
            set { _referencePathTypeName = value; }
        }


        /// <summary>
        /// IntermediateOutputPath
        ///
        /// The value which is set to IntermediateOutputPath property in current project file.
        ///
        /// Passing this value explicitly is to make sure to generate temporary target assembly
        /// in expected directory.
        /// </summary>
        [Required]
        public string IntermediateOutputPath
        {
            get { return _intermediateOutputPath; }
            set { _intermediateOutputPath = value; }
        }

        /// <summary>
        /// AssemblyName
        ///
        /// The value which is set to AssemblyName property in current project file.
        /// Passing this value explicitly is to make sure to generate the expected
        /// temporary target assembly.
        ///
        /// </summary>
        [Required]
        public string AssemblyName
        {
            get { return _assemblyName; }
            set { _assemblyName = value; }
        }

        /// <summary>
        /// CompileTargetName
        ///
        /// The msbuild target name which is used to generate assembly from source code files.
        /// Usually it is "CoreCompile"
        ///
        /// </summary>
        [Required]
        public string CompileTargetName
        {
            get { return _compileTargetName; }
            set { _compileTargetName = value; }
        }

        /// <summary>
        /// Optional <see cref="Boolean"/> task parameter
        ///
        /// When <code>true</code>, debugging information is enabled for the <see cref="GenerateTemporaryTargetAssembly"/>
        /// Task. At this time, the only debugging information that is generated consists of the temporary project that is
        /// created to generate the temporary target assembly. This temporary project is normally deleted at the end of this
        /// MSBUILD task; when <see cref="GenerateTemporaryTargetAssemblyDebuggingInformation"/> is enable, this temporary project
        /// will be retained for inspection by the developer.
        ///
        /// This is a diagnostic parameter, and it defaults to <code>false</code>.
        /// </summary>
        public bool GenerateTemporaryTargetAssemblyDebuggingInformation
        {
            get { return _generateTemporaryTargetAssemblyDebuggingInformation; }
            set { _generateTemporaryTargetAssemblyDebuggingInformation = value; }
        }

        /// <summary>
        /// Analyzers
        ///
        /// Required for Source Generator support. May be null.
        ///
        /// </summary>
        public ITaskItem[] Analyzers
        { get; set; }

        /// <summary>
        /// AnalyzerTypeName
        ///   The appropriate item name which can be accepted by managed compiler task.
        ///   It is "Analyzer" for now.
        ///
        ///   Adding this property is to make the type name configurable, if it is changed,
        ///   No code is required to change in this task, but set a new type name in project file.
        /// </summary>
        [Required]
        public string AnalyzerTypeName { get; set; }

        /// <summary>
        /// RootNamespace
        ///
        /// Required for Source Generator support. May be null.
        ///
        /// </summary>
        public string RootNamespace { get; set; }

        /// <summary>
        /// BaseIntermediateOutputPath
        ///
        /// Required for Source Generator support. May be null.
        ///
        /// </summary>
        public string BaseIntermediateOutputPath
        {
            get; set;
        }

        /// <summary>
        /// IncludePackageReferencesDuringMarkupCompilation
        ///
        /// Required for Source Generator support. May be null.
        ///
        /// Set this property to 'false' to use the .NET Core 3.0 behavior for this task.
        ///
        /// </summary>
        public string IncludePackageReferencesDuringMarkupCompilation
        { get; set; }

        /// <summary>
        /// TemporaryTargetAssemblyProjectName
        ///
        /// Required for PackageReference support.
        ///
        /// This property may be null if 'IncludePackageReferencesDuringMarkupCompilation' is 'false'.
        ///
        /// The file name with extension of the randomly generated project name for the temporary assembly
        ///
        /// </summary>
        public string TemporaryTargetAssemblyProjectName
        { get; set; }

        /// <summary>
        ///
        /// MSBuildProjectExtensionsPath
        ///
        /// Required for PackageReference support.
        ///
        /// MSBuildProjectExtensionsPath may be overridden and must be passed into the temporary project.
        ///
        /// This is required for some VS publishing scenarios.
        ///
        /// </summary>
        public string MSBuildProjectExtensionsPath
        { get; set; }

        /// <summary>
        ///
        /// TemporaryAssemblyForLocalTypeReference
        ///
        /// The path of the generated temporary local type assembly.
        ///
        /// </summary>
        [Output]
        public string TemporaryAssemblyForLocalTypeReference
        { get; set; }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        //
        // Remove specific entity from project file.
        //
        private void RemoveEntityByName(XmlDocument xmlProjectDoc, string sItemName, string groupName)
        {

            if (xmlProjectDoc == null || String.IsNullOrEmpty(sItemName))
            {
                // When the parameters are not valid, simply return it, instead of throwing exceptions.
                return;
            }

            //
            // The project file format is always like below:
            //
            //  <Project  xmlns="...">
            //     <ProjectGroup>
            //         ......
            //     </ProjectGroup>
            //
            //     ...
            //     <ItemGroup>
            //         <ItemNameHere ..../>
            //         ....
            //     </ItemGroup>
            //
            //     ....
            //     <Import ... />
            //     ...
            //     <Target Name="xxx" ..../>
            //
            //      ...
            //
            //  </Project>
            //
            //
            // The order of children nodes under Project Root element is random
            //

            XmlNode root = xmlProjectDoc.DocumentElement;

            if (!root.HasChildNodes)
            {
                // If there is no child element in this project file, just return immediatelly.
                return;
            }

            for (int i = 0; i < root.ChildNodes.Count; i++)
            {
                if (root.ChildNodes[i] is XmlElement nodeGroup && string.Equals(nodeGroup.Name, groupName, StringComparison.OrdinalIgnoreCase))
                {
                    //
                    // This is ItemGroup element.
                    //
                    if (nodeGroup.HasChildNodes)
                    {
                        ArrayList itemToRemove = new ArrayList();

                        for (int j = 0; j < nodeGroup.ChildNodes.Count; j++)
                        {
                            if (nodeGroup.ChildNodes[j] is XmlElement nodeItem && string.Equals(nodeItem.Name, sItemName, StringComparison.OrdinalIgnoreCase))
                            {
                                // This is the item that need to remove.
                                // Add it into the temporary array list.
                                // Don't delete it here, since it would affect the ChildNodes of parent element.
                                //
                                itemToRemove.Add(nodeItem);
                            }
                        }

                        //
                        // Now it is the right time to delete the elements.
                        //
                        if (itemToRemove.Count > 0)
                        {
                            foreach (object node in itemToRemove)
                            {
                                //
                                // Remove this item from its parent node.
                                // the parent node should be nodeGroup.
                                //
                                if (node is XmlElement item)
                                {
                                    nodeGroup.RemoveChild(item);
                                }
                            }
                        }
                    }

                    //
                    // Removed all the items with given name from this item group.
                    //
                    // Continue the loop for the next ItemGroup.
                }

            }   // end of "for i" statement.
        }

        //
        // Remove specific items from project file. Every item should be under an ItemGroup.
        //
        private void RemoveItemsByName(XmlDocument xmlProjectDoc, string sItemName)
        {
            RemoveEntityByName(xmlProjectDoc, sItemName, ITEMGROUP_NAME);
        }

        //
        // Remove specific property from project file. Every property should be under an PropertyGroup.
        //
        private void RemovePropertiesByName(XmlDocument xmlProjectDoc, string sPropertyName)
        {
            RemoveEntityByName(xmlProjectDoc, sPropertyName, PROPERTYGROUP_NAME);
        }

        //
        // Add a list of files into an Item in the project file, the ItemName is specified by sItemName.
        //
        private void AddNewItems(XmlDocument xmlProjectDoc, string sItemName, ITaskItem[] pItemList)
        {
            if (xmlProjectDoc == null || String.IsNullOrEmpty(sItemName) || pItemList == null)
            {
                // When the parameters are not valid, simply return it, instead of throwing exceptions.
                return;
            }

            XmlNode root = xmlProjectDoc.DocumentElement;

            // Create a new ItemGroup element
            XmlNode nodeItemGroup = xmlProjectDoc.CreateElement(ITEMGROUP_NAME, root.NamespaceURI);

            // Append this new ItemGroup item into the list of children of the document root.
            root.AppendChild(nodeItemGroup);

            XmlElement embedItem = null;

            for (int i = 0; i < pItemList.Length; i++)
            {
                // Create an element for the given sItemName
                XmlElement nodeItem = xmlProjectDoc.CreateElement(sItemName, root.NamespaceURI);

                // Create an Attribute "Include"
                XmlAttribute attrInclude = xmlProjectDoc.CreateAttribute(INCLUDE_ATTR_NAME);

                ITaskItem pItem = pItemList[i];

                // Set the value for Include attribute.
                attrInclude.Value = pItem.ItemSpec;

                // Add the attribute to current item node.
                nodeItem.SetAttributeNode(attrInclude);

                string embedInteropTypesMetadata = pItem.GetMetadata(EMBEDINTEROPTYPES);
                if (!String.IsNullOrEmpty(embedInteropTypesMetadata))
                {
                    embedItem = xmlProjectDoc.CreateElement(EMBEDINTEROPTYPES, root.NamespaceURI);
                    embedItem.InnerText = embedInteropTypesMetadata;
                    nodeItem.AppendChild(embedItem);
                }

                string aliases = pItem.GetMetadata(ALIASES);
                if (!String.IsNullOrEmpty(aliases))
                {
                    embedItem = xmlProjectDoc.CreateElement(ALIASES, root.NamespaceURI);
                    embedItem.InnerText = aliases;
                    nodeItem.AppendChild(embedItem);
                }

                // Add current item node into the children list of ItemGroup
                nodeItemGroup.AppendChild(nodeItem);
            }
        }

        private void AddNewProperties(XmlDocument xmlProjectDoc, List<(string PropertyName, string PropertyValue)> properties )
        {
            if (xmlProjectDoc == null || properties == null )
            {
                // When the parameters are not valid, simply return it, instead of throwing exceptions.
                return;
            }

            XmlNode root = xmlProjectDoc.DocumentElement;

            // Create a new PropertyGroup element
            XmlNode nodeItemGroup = xmlProjectDoc.CreateElement("PropertyGroup", root.NamespaceURI);
            root.PrependChild(nodeItemGroup);

            // Append this new ItemGroup item into the list of children of the document root.
            foreach(var property in properties)
            {
                // Skip empty properties
                if (!string.IsNullOrEmpty(property.PropertyValue))
                {
                    // Create an element for the given propertyName
                    XmlElement nodeItem = xmlProjectDoc.CreateElement(property.PropertyName, root.NamespaceURI);
                    nodeItem.InnerText = property.PropertyValue;

                    // Add current item node into the PropertyGroup
                    nodeItemGroup.AppendChild(nodeItem);
                }
            }
        }

        //
        // Replace implicit SDK imports with explicit imports
        //
        private static void ReplaceImplicitImports(XmlDocument xmlProjectDoc)
        {
            if (xmlProjectDoc == null)
            {
                // When the parameters are not valid, simply return it, instead of throwing exceptions.
                return;
            }

            XmlNode root = xmlProjectDoc.DocumentElement;

            for (int i = 0; i < root.Attributes.Count; i++)
            {
                XmlAttribute xmlAttribute = root.Attributes[i];

                if (xmlAttribute.Name.Equals("Sdk", StringComparison.OrdinalIgnoreCase))
                {
                    string sdks = xmlAttribute.Value;

                    bool removedSdkAttribute = false;
                    XmlNode previousNodeImportProps = null;
                    XmlNode previousNodeImportTargets = null;

                    foreach (string sdk in sdks.Split(s_semicolonChar).Select(i => i.Trim()))
                    {
                        //  <Project Sdk="Microsoft.NET.Sdk">
                        //  <Project Sdk="My.Custom.Sdk/1.0.0">
                        //  <Project Sdk="My.Custom.Sdk/min=1.0.0">
                        if (!SdkReference.TryParse(sdk, out SdkReference sdkReference))
                            break;

                        // Remove Sdk attribute
                        if (!removedSdkAttribute)
                        {
                            root.Attributes.Remove(xmlAttribute);

                            removedSdkAttribute = true;
                        }

                        //
                        // Add explicit top import
                        //
                        //  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
                        //  <Import Project="Sdk.props" Sdk="My.Custom.Sdk" Version="1.0.0" />
                        //  <Import Project="Sdk.props" Sdk="My.Custom.Sdk" MinimumVersion="1.0.0" />
                        //
                        XmlNode nodeImportProps = CreateImportProjectSdkNode(xmlProjectDoc, "Sdk.props", sdkReference);

                        // Prepend this Import to the root of the XML document
                        if (previousNodeImportProps == null)
                        {
                            previousNodeImportProps = root.PrependChild(nodeImportProps);
                        }
                        else
                        {
                            previousNodeImportProps = root.InsertAfter(nodeImportProps, previousNodeImportProps);
                        }

                        //
                        // Add explicit bottom import
                        //
                        //  <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
                        //  <Import Project="Sdk.targets" Sdk="My.Custom.Sdk" Version="1.0.0" />
                        //  <Import Project="Sdk.targets" Sdk="My.Custom.Sdk" MinimumVersion="1.0.0" />
                        //
                        XmlNode nodeImportTargets = CreateImportProjectSdkNode(xmlProjectDoc, "Sdk.targets", sdkReference);

                        // Append this Import to the end of the XML document
                        if (previousNodeImportTargets == null)
                        {
                            previousNodeImportTargets = root.AppendChild(nodeImportTargets);
                        }
                        else
                        {
                            previousNodeImportTargets = root.InsertAfter(nodeImportTargets, previousNodeImportTargets);
                        }
                    }
                }
            }
        }

        // Creates an XmlNode that contains an Import Project element
        //
        //  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
        private static XmlNode CreateImportProjectSdkNode(XmlDocument xmlProjectDoc, string projectAttributeValue, SdkReference sdkReference)
        {
            XmlNode nodeImport = xmlProjectDoc.CreateElement("Import", xmlProjectDoc.DocumentElement.NamespaceURI);
            XmlAttribute projectAttribute = xmlProjectDoc.CreateAttribute("Project");
            projectAttribute.Value = projectAttributeValue;
            XmlAttribute sdkAttributeProps = xmlProjectDoc.CreateAttribute("Sdk");
            sdkAttributeProps.Value = sdkReference.Name;
            nodeImport.Attributes.Append(projectAttribute);
            nodeImport.Attributes.Append(sdkAttributeProps);

            if (!string.IsNullOrEmpty(sdkReference.Version))
            {
                XmlAttribute sdkVersionAttributeProps = xmlProjectDoc.CreateAttribute("Version");
                sdkVersionAttributeProps.Value = sdkReference.Version;
                nodeImport.Attributes.Append(sdkVersionAttributeProps);
            }

            if (!string.IsNullOrEmpty(sdkReference.MinimumVersion))
            {
                XmlAttribute sdkVersionAttributeProps = xmlProjectDoc.CreateAttribute("MinimumVersion");
                sdkVersionAttributeProps.Value = sdkReference.MinimumVersion;
                nodeImport.Attributes.Append(sdkVersionAttributeProps);
            }

            return nodeImport;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private string _currentProject = String.Empty;

        private ITaskItem[] _generatedCodeFiles;
        private ITaskItem[] _referencePath;

        private string _referencePathTypeName;
        private string _compileTypeName;

        private string _msbuildBinPath;

        private string  _intermediateOutputPath;
        private string  _assemblyName;
        private string  _compileTargetName;
        private bool _generateTemporaryTargetAssemblyDebuggingInformation = false;

        private const string intermediateOutputPathPropertyName = "IntermediateOutputPath";
        private const string assemblyNamePropertyName = "AssemblyName";
        private const string targetAssemblyProjectNamePropertyName = "_TargetAssemblyProjectName";

        private const string ALIASES = "Aliases";
        private const string REFERENCETYPENAME = "Reference";
        private const string EMBEDINTEROPTYPES = "EmbedInteropTypes";
        private const string APPDEFNAME = "ApplicationDefinition";
        private const string PAGENAME = "Page";
        private const string MARKUPRESOURCENAME = "MarkupResource";
        private const string RESOURCENAME = "Resource";

        private const string ITEMGROUP_NAME = "ItemGroup";
        private const string PROPERTYGROUP_NAME = "PropertyGroup";
        private const string INCLUDE_ATTR_NAME = "Include";

        private const string WPFTMP = "wpftmp";

        private static readonly char[] s_semicolonChar = [';'];

        #endregion Private Fields

    }

    #endregion GenerateProjectForLocalTypeReference Task class
}


