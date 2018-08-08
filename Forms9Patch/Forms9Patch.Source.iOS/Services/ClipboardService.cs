using System;
using Xamarin.Forms;
using UIKit;
using Foundation;
using MobileCoreServices;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms.Platform.iOS;
using System.IO;
using System.Diagnostics;

[assembly: Dependency(typeof(Forms9Patch.iOS.ClipboardService))]
namespace Forms9Patch.iOS
{
    public class ClipboardService : Forms9Patch.IClipboardService
    {
        const bool TestPre11 = true;

        static internal NSString TypeListUti = new NSString(UTType.CreatePreferredIdentifier(UTType.TagClassMIMEType, "application/f9p-clipboard-typelist", null));

        static ClipboardService()
        {
            UIPasteboard.Notifications.ObserveChanged(OnPasteboardChanged);
            UIPasteboard.Notifications.ObserveRemoved(OnPasteboardChanged);
        }

        static void OnPasteboardChanged(object sender, UIPasteboardChangeEventArgs e)
        {
            Forms9Patch.Clipboard.OnContentChanged(null, EventArgs.Empty);
        }

        #region Entry property
        nint _lastEntryChangeCount = int.MinValue;
        IClipboardEntry _lastEntry;
        public IClipboardEntry Entry
        {
            get
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                if (_lastEntryChangeCount != UIPasteboard.General.ChangeCount)
                {
                    System.Diagnostics.Debug.WriteLine("NOT CACHED: _lastEntryChangeCount=[" + _lastEntryChangeCount + "] ChangeCount=[" + UIPasteboard.General.ChangeCount + "]");
                    _lastEntry = new ClipboardEntry();
                }
                _lastEntryChangeCount = UIPasteboard.General.ChangeCount;
                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine("\t\t ClipboardService get_Entry elapsed: " + stopwatch.ElapsedMilliseconds);
                return _lastEntry;
            }
            set
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                if (value is Forms9Patch.ClipboardEntry entry)
                {

                    if (!TestPre11 && Forms9Patch.OsInfoService.Version >= new Version(11, 0))
                    {
                        var itemProviders = new List<NSItemProvider>();
                        foreach (var mimeItem in entry.Items)
                        {
                            if (mimeItem.MimeType?.ToNsUti() is NSString nsUti)
                            {
                                NSItemProvider itemProvider = null;
                                if (mimeItem.Value is Uri uri)
                                {
                                    var nsUri = new NSUrl(uri.AbsoluteUri);
                                    itemProvider = new NSItemProvider(nsUri);
                                }
                                else if (mimeItem.Value is FileInfo fileInfo)
                                {
                                    var nsUri = new NSUrl(fileInfo.FullName);
                                    itemProvider = new NSItemProvider(nsUri);
                                }
                                else if (mimeItem.Value.ToNSObject() is NSObject nsObject)
                                    itemProvider = new NSItemProvider(nsObject, nsUti);
                                if (itemProvider != null)
                                    itemProviders.Add(itemProvider);
                            }
                        }
                        if (EntryCaching)
                        {
                            _lastEntry = value;
                            _lastEntryChangeCount = UIPasteboard.General.ChangeCount + 1;
                        }
                        UIPasteboard.General.ItemProviders = itemProviders.ToArray();
                    }
                    else
                    {
                        var items = new List<NSMutableDictionary>();
                        NSMutableDictionary itemRenditions = null;
                        //NSMutableDictionary itemCSharpTypeMap = null;
                        foreach (var mimeItem in entry.Items)
                        {
                            System.Diagnostics.Debug.WriteLine("\t\t ClipboardService set_Entry 1.1 elapsed: " + stopwatch.ElapsedMilliseconds);
                            if (mimeItem.MimeType?.ToNsUti() is NSString nsUti && mimeItem.Value.ToNSObject() is NSObject nSObject)
                            {
                                /*
                                Type itemCsharpType = null;
                                if (mimeItem.MimeType.StartsWith("image/", StringComparison.InvariantCultureIgnoreCase))
                                    itemCsharpType = nSObject is NSData ? typeof(byte[]) : typeof(Uri);
                                else
                                {
                                    //nSObject = dataItem.KeyedArchiver;
                                    //nsUti = dataItem.NSUti;
                                    //itemType = item.Type;
                                    itemCsharpType = mimeItem.Type;
                                }
                                */
                                foreach (var item in items)
                                {
                                    if (!item.Any((kvp) => ((NSString)kvp.Key) == nsUti))
                                    {
                                        itemRenditions = item;
                                        break;
                                    }
                                }
                                System.Diagnostics.Debug.WriteLine("\t\t ClipboardService set_Entry 1.2 elapsed: " + stopwatch.ElapsedMilliseconds);

                                if (itemRenditions == null)
                                {
                                    itemRenditions = new NSMutableDictionary();
                                    items.Add(itemRenditions);
                                }
                                System.Diagnostics.Debug.WriteLine("\t\t ClipboardService set_Entry 1.3 elapsed: " + stopwatch.ElapsedMilliseconds);

                                /*
                                if (mimeItem.MimeType.StartsWith("image/", StringComparison.InvariantCultureIgnoreCase))
                                    itemRenditions.Add(nsUti, nSObject);
                                else
                                {
                                    var archiver = NSKeyedArchiver.ArchivedDataWithRootObject(nSObject);
                                    itemRenditions.Add(nsUti, archiver);
                                }
                                */

                                var plist = NSPropertyListSerialization.DataWithPropertyList(nSObject, NSPropertyListFormat.Binary, NSPropertyListWriteOptions.Immutable, out NSError nSError);
                                System.Diagnostics.Debug.WriteLine("\t\t ClipboardService set_Entry 1.4 elapsed: " + stopwatch.ElapsedMilliseconds);
                                itemRenditions.Add(nsUti, plist);
                                System.Diagnostics.Debug.WriteLine("\t\t ClipboardService set_Entry 1.5 elapsed: " + stopwatch.ElapsedMilliseconds);


                                //itemRenditions.Add(nsUti, nSObject);
                            }
                        }
                        var array = items.ToArray();
                        System.Diagnostics.Debug.WriteLine("\t\t ClipboardService set_Entry 2.1 elapsed: " + stopwatch.ElapsedMilliseconds);
                        if (EntryCaching)
                        {
                            _lastEntry = value;
                            _lastEntryChangeCount = UIPasteboard.General.ChangeCount + 1;
                        }
                        UIPasteboard.General.Items = array;
                        System.Diagnostics.Debug.WriteLine("\t\t ClipboardService set_Entry 2.2 elapsed: " + stopwatch.ElapsedMilliseconds);
                    }

                }

                stopwatch.Stop();
            }
        }

        public bool EntryCaching { get; set; } = false;


        #endregion

    }


}