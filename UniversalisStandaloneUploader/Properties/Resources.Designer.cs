﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace UniversalisStandaloneUploader.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("UniversalisStandaloneUploader.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to Do you want to stop uploading market board data?.
        /// </summary>
        internal static string AskStopUploadingData {
            get {
                return ResourceManager.GetString("AskStopUploadingData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Thank you for using the Universalis uploader!
        ///
        ///Please don&apos;t forget to whitelist the uploader in your windows firewall, like you would with ACT.
        ///It will not be able to process market board data otherwise.
        ///To start uploading, log in with your character..
        /// </summary>
        internal static string FirstLaunchWelcome {
            get {
                return ResourceManager.GetString("FirstLaunchWelcome", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Bitmap.
        /// </summary>
        internal static System.Drawing.Bitmap universalis_bodge {
            get {
                object obj = ResourceManager.GetObject("universalis_bodge", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Universalis Uploader.
        /// </summary>
        internal static string UniversalisFormTitle {
            get {
                return ResourceManager.GetString("UniversalisFormTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Universalis uploader needs to be updated. Please download an updated version from the GitHub releases page..
        /// </summary>
        internal static string UniversalisNeedsUpdateLong {
            get {
                return ResourceManager.GetString("UniversalisNeedsUpdateLong", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Universalis Uploader Update.
        /// </summary>
        internal static string UniversalisNeedsUpdateLongCaption {
            get {
                return ResourceManager.GetString("UniversalisNeedsUpdateLongCaption", resourceCulture);
            }
        }
    }
}
