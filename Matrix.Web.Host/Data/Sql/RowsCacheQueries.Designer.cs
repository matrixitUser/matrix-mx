﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Matrix.Web.Host.Data.Sql {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class RowsCacheQueries {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal RowsCacheQueries() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Matrix.Web.Host.Data.Sql.RowsCacheQueries", typeof(RowsCacheQueries).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to create table #tmp12(id uniqueidentifier)
        ///insert into #tmp12(id)values {0}
        ///select t.id from RowsCache r right join #tmp12 t on r.id=t.id where r.id is null
        ///drop table #tmp12.
        /// </summary>
        internal static string check {
            get {
                return ResourceManager.GetString("check", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to if object_id(&apos;RowsCache&apos;,&apos;U&apos;) is null 
        ///create table RowsCache( 
        ///	[id] uniqueidentifier not null primary key,
        ///	[state] int,
        ///	[description] nvarchar(255),
        ///	[name] nvarchar(255),
        ///	[city] nvarchar(255),
        ///	[phone] nvarchar(255),
        ///	[imei] nvarchar(255)
        ///)
        ///.
        /// </summary>
        internal static string create {
            get {
                return ResourceManager.GetString("create", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to if not exists(select * from RowsCache where id=@id)
        ///	insert into RowsCache([id],[state],[description],[name],[city],[phone],[imei])
        ///	values(@id,@state,@description,@name,@city,@phone,@imei)
        ///else
        ///	update RowsCache set [name]=isnull(@name,name),[state]=isnull(@state,[state]),[description]=isnull(@description,[description]),
        ///	[city]=isnull(@city,city),[phone]=isnull(@phone,phone),[imei]=isnull(@imei,imei)
        ///	where id=@id.
        /// </summary>
        internal static string update {
            get {
                return ResourceManager.GetString("update", resourceCulture);
            }
        }
    }
}