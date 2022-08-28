
// test, zanim to trafi do Uno

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Linq;
#if false

namespace BeforeUno
{
#region "myDumps"

	class MyDUmps
	{
		static bool _bWasTypes = false;

		public static string DumpTableHeaderNames(Android.Database.ICursor _cursor)
		{
			string sDump = "";
			if (_cursor != null)
			{
				sDump = sDump + "Row count: " + _cursor.Count.ToString() + "\n";
				sDump = sDump + "Column count: " + _cursor.ColumnCount.ToString() + "\n\n";
				for (int i = 0; i < _cursor.ColumnCount; i++)
				{
					sDump = sDump + "|" + _cursor.GetColumnName(i);
				}
				sDump = sDump + "\n";
				//for (int i = 0; i < _cursor.ColumnCount; i++)
				//{
				//	sDump = sDump + _cursor.GetType(i);
				//}
				//sDump = sDump + "\n";
			}
			_bWasTypes = false;
			return sDump;
		}

		public static string DumpTableHeaderTypes(Android.Database.ICursor _cursor)
		{
			string sDump = "";

			if (_cursor.IsBeforeFirst)
				return sDump;   // jeszcze nie mozemy

			for (int i = 0; i < _cursor.ColumnCount; i++)
			{
				switch(_cursor.GetType(i))
				{
					case Android.Database.FieldType.Blob:
						sDump = sDump + "|blob";
						break;
					case Android.Database.FieldType.Float:
						sDump = sDump + "|float";
						break;
					case Android.Database.FieldType.Integer:
						sDump = sDump + "|int";
						break;
					case Android.Database.FieldType.Null :
						sDump = sDump + "|null";
						break;
					case Android.Database.FieldType.String :
						sDump = sDump + "|string";
						break;
					default:
						sDump = sDump + "|UNKNOWN";
						break;
				}
			}
			sDump = sDump + "\n\n";


			_bWasTypes = true;
			return sDump;
		}
		public static string DumpTableRows(Android.Database.ICursor _cursor)
		{
			string sDump = "";

			for (int pageGuard = 100; pageGuard > 0 && _cursor.MoveToNext(); pageGuard--)
			{

				if (!_bWasTypes)
					sDump = DumpTableHeaderTypes(_cursor);

				for (int i = 0; i < _cursor.ColumnCount; i++)
				{
					switch (_cursor.GetType(i))
					{
						case Android.Database.FieldType.Blob:
							sDump = sDump + "|<blob>";
							break;
						case Android.Database.FieldType.Float:
							try
							{
								sDump = sDump + "|" + _cursor.GetFloat(i).ToString();
							}
							catch
							{
								sDump = sDump + "|<error>";
							}
							break;
						case Android.Database.FieldType.Integer:
							try
							{ 
							sDump = sDump + "|" + _cursor.GetInt(i).ToString();
							}
							catch
							{
								sDump = sDump + "|<error>";
							}

							break;
						case Android.Database.FieldType.Null:
							sDump = sDump + "|null";
							break;
						case Android.Database.FieldType.String:
							try
							{ 
							sDump = sDump + "|" + _cursor.GetString(i);
							}
							catch
							{
								sDump = sDump + "|<error>";
							}

							break;
						default:
							sDump = sDump + "|UNKNOWN";
							break;
					}
				}
				sDump = sDump + "\n";

			}

			return sDump;

		}

	}
#endregion

#region "contact"
	public enum ContactStoreAccessType
	{
		AppContactsReadWrite,
		AllContactsReadOnly,
		AllContactsReadWrite,
	}

	public partial class ContactManager
	{

		public static IAsyncOperation<ContactStore> RequestStoreAsync() => RequestStoreAsync(ContactStoreAccessType.AllContactsReadOnly);

		public static IAsyncOperation<ContactStore> RequestStoreAsync(ContactStoreAccessType accessType) => RequestStoreAsyncTask(accessType).AsAsyncOperation<ContactStore>();
		//{
		//	return RequestStoreAsyncTask(accessType).AsAsyncOperation<ContactStore>();
		//}

		private static async Task<ContactStore> RequestStoreAsyncTask(ContactStoreAccessType accessType)
		{
			// UWP: AppContactsReadWrite, AllContactsReadOnly, AllContactsReadWrite (cannot be used without special provisioning by Microsoft)
			// Android: Manifest has READ_CONTACTS and WRITE_CONTACTS, no difference between app/limited/full
			// using only AllContactsReadOnly, other: throw NotImplementedException

			if (accessType != ContactStoreAccessType.AllContactsReadOnly)
				throw new NotImplementedException();

			// do we have declared this permission in Manifest?
			// it could be also Coarse, without GPS
			Android.Content.Context context = Android.App.Application.Context;
			Android.Content.PM.PackageInfo packageInfo =
				context.PackageManager.GetPackageInfo(context.PackageName, Android.Content.PM.PackageInfoFlags.Permissions);
			var requestedPermissions = packageInfo?.RequestedPermissions;
			if (requestedPermissions is null)
				return null;

			bool bInManifest = requestedPermissions.Any(p => p.Equals(Android.Manifest.Permission.ReadContacts, StringComparison.OrdinalIgnoreCase));// false;
			//bool bInManifest = false; // 
			//foreach (string oPerm in requestedPermissions)
			//{
			//	if (oPerm.Equals(Android.Manifest.Permission.ReadContacts, StringComparison.OrdinalIgnoreCase))
			//		bInManifest = true;
			//}

			if (!bInManifest)
				return null;


			// check if permission is granted
			if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Uno.UI.ContextHelper.Current, Android.Manifest.Permission.ReadContacts)
					== Android.Content.PM.Permission.Granted)
			{
				return new ContactStore();
			}


			// system dialog asking for permission

			// this code would not compile here - but it compile in your own app.
			// to be compiled inside Uno, it has to be splitted into layers
			var tcs = new TaskCompletionSource<Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs>();

			void handler(object sender, Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs e)
			{

				if (e.RequestCode == 1)
				{
					tcs.TrySetResult(e);
				}
			}

			var current = Uno.UI.BaseActivity.Current;

			try
			{
				current.RequestPermissionsResultWithResults += handler;

				Android.Support.V4.App.ActivityCompat.RequestPermissions(Uno.UI.BaseActivity.Current, 
					new[] { Android.Manifest.Permission.ReadContacts }, 1);

				var result = await tcs.Task;
				if (result.GrantResults.Length < 1)
					return null;
				if (result.GrantResults[0] == Android.Content.PM.Permission.Granted)
					return new ContactStore();

			}
			finally
			{
				current.RequestPermissionsResultWithResults -= handler;
			}

			return null;

		}

	}

	public partial class ContactStore
	{
		
		public ContactReader GetContactReader() => GetContactReader(new ContactQueryOptions("", ContactQuerySearchFields.None));

		public ContactReader GetContactReader(ContactQueryOptions options)
		{
			return new ContactReader(options);
		}

	}

	[Flags]
	public enum ContactQuerySearchFields
	{
		None = 0,   // no search - all entries
		Name = 1,
		Email = 2,
		Phone = 4,
		All = -1 // 4294967295 == 0b_1111_1111_1111_1111_1111_1111_1111_1111 == ‭FFFFFFFF‬
	}

	[Flags]
	public enum ContactQueryDesiredFields
	{
		None = 0,
		PhoneNumber = 1,
		EmailAddress = 2,
		PostalAddress = 4
	}

	public partial class ContactQueryOptions
	{   // https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.contacts.contactqueryoptions.-ctor

		internal ContactQuerySearchFields _whereToSearch;
		internal string _whatToSearch;

		public ContactQueryDesiredFields DesiredFields { get; set; }

		public ContactQueryOptions(string text) // => ContactQueryOptions(text, ContactQuerySearchFields.All);
		{
			_whatToSearch = text;
			_whereToSearch = ContactQuerySearchFields.All;
		}

		public ContactQueryOptions(string text, ContactQuerySearchFields fields)
		{
			_whatToSearch = text;
			_whereToSearch = fields;
		}

		public ContactQueryOptions()
		{
			_whatToSearch = "";
			_whereToSearch = ContactQuerySearchFields.None;
		}

	}

	public partial class ContactReader
	{
		internal ContactQueryOptions _queryOptions;
		private Android.Database.ICursor _cursor = null;
		private Android.Content.ContentResolver _contentResolver = null;

		internal ContactReader(ContactQueryOptions options)
		{
			if (options is null)
				throw new ArgumentNullException();

			_queryOptions = options;

			Android.Net.Uri oUri;
			string sColumnIdName = "_id";

			switch(options._whereToSearch)
			{
				case ContactQuerySearchFields.Phone:
					oUri = Android.Net.Uri.WithAppendedPath(
						Android.Provider.ContactsContract.PhoneLookup.ContentFilterUri, // jego Phone.Contact_ID to .Contacts._ID 
						Android.Net.Uri.Encode(options._whatToSearch));
					sColumnIdName = "contact_id";
					break;
				case ContactQuerySearchFields.Name:
					oUri = Android.Net.Uri.WithAppendedPath(
						Android.Provider.ContactsContract.Contacts.ContentFilterUri,
						Android.Net.Uri.Encode(options._whatToSearch)); 
					break;
				default:
					oUri = Android.Provider.ContactsContract.Contacts.ContentUri; // ich _ID == Phone.Contact_ID
					break;
			}
			
			// filtr moglby byc... ale wedle czego?
			// ewentualnie wedle substring DisplayName

			_contentResolver = Android.App.Application.Context.ContentResolver;

			_cursor = _contentResolver.Query(oUri,
									new string[] { sColumnIdName, "display_name" },  // which columns
									null,	// where
									null,	// null
									null);   // == date DESC

		}

		public IAsyncOperation<ContactBatch> ReadBatchAsync()
		{
			ContactBatch batch = new ContactBatch(ReadBatchInternal());
			return null;
		}

		internal List<Contact> ReadBatchInternal()
		{
			var entriesList = new List<Contact>();

			if (_cursor is null)
			{
				return entriesList;
			}


			ContactQueryDesiredFields desiredFields = _queryOptions.DesiredFields;
			// default value (==None) treat as "all"
			if (desiredFields == ContactQueryDesiredFields.None)
				desiredFields = ContactQueryDesiredFields.EmailAddress | ContactQueryDesiredFields.PhoneNumber | ContactQueryDesiredFields.PostalAddress;

			// add fields we search by
			if (_queryOptions._whereToSearch.HasFlag(ContactQuerySearchFields.Email))
				desiredFields |= ContactQueryDesiredFields.EmailAddress;
			if (_queryOptions._whereToSearch.HasFlag(ContactQuerySearchFields.Phone))
				desiredFields |= ContactQueryDesiredFields.PhoneNumber;

			for (int pageGuard = 100; pageGuard > 0 && _cursor.MoveToNext(); pageGuard--)
			{
				var entry = new Contact();
				int contactId = _cursor.GetInt(0);  // we defined columns while opening cursor, so we know what data is in which columns

				entry.DisplayName = _cursor.GetString(1);   // we defined columns while opening cursor, so we know what data is in which columns

				bool searchFound = false; // should it be included in result set
				if (_queryOptions._whereToSearch == ContactQuerySearchFields.None ||	// no filtering at all
					_queryOptions._whereToSearch == ContactQuerySearchFields.Phone ||	// filtering done by Android
					_queryOptions._whereToSearch == ContactQuerySearchFields.Name)      // filtering done by Android
							searchFound = true; // include in result - and skip tests

				if (!searchFound && _queryOptions._whereToSearch.HasFlag(ContactQuerySearchFields.Name))
				{
					if (entry.DisplayName.Contains(_queryOptions._whatToSearch))
						searchFound = true;
				}



				// filling properties, using other tables


				// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.Phone
				// NUMBER, TYPE
				entry.Phones.Clear();
				if (desiredFields.HasFlag(ContactQueryDesiredFields.PhoneNumber))
				{
					Android.Database.ICursor subCursor = _contentResolver.Query(
										Android.Provider.ContactsContract.Data.ContentUri,
										new string[] { "data1", "data2" }, //null,   // all columns
																		   // ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
										"contact_id = ? AND mimetype = ?",
										new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.Phone.ContentItemType },
										null);   // default order

					//int columnD1 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data1); // Phone.NUMBER
					//int columnD2 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data2); // Phone.TYPE

					for (int itemGuard = 10; itemGuard > 0 && subCursor.MoveToNext(); itemGuard--)
					{
						var itemEntry = new ContactPhone();
						itemEntry.Number = subCursor.GetString(0);   // we defined columns while opening cursor, so we know what data is in which columns

						if (!searchFound && _queryOptions._whereToSearch.HasFlag(ContactQuerySearchFields.Phone))
						{
							if (itemEntry.Number.Contains(_queryOptions._whatToSearch))
								searchFound = true;
						}

						switch (subCursor.GetInt(1))    // we defined columns while opening cursor, so we know what data is in which columns
						{
							case 1:
								itemEntry.Kind = ContactPhoneKind.Home;
								break;
							case 2:
								itemEntry.Kind = ContactPhoneKind.Mobile;
								break;
							case 3:
								itemEntry.Kind = ContactPhoneKind.Work;
								break;
							case 6:
								itemEntry.Kind = ContactPhoneKind.Pager;
								break;
							case 4:
								itemEntry.Kind = ContactPhoneKind.BusinessFax;
								break;
							case 5:
								itemEntry.Kind = ContactPhoneKind.HomeFax;
								break;
							case 10:
								itemEntry.Kind = ContactPhoneKind.Company;
								break;
							case 19:
								itemEntry.Kind = ContactPhoneKind.Assistant;
								break;
							case 14:
								itemEntry.Kind = ContactPhoneKind.Radio;
								break;
							default:    // TYPE_CALLBACK, TYPE_CAR, TYPE_ISDN, TYPE_MAIN, TYPE_MMS, TYPE_OTHER, TYPE_OTHER_FAX, TYPE_PAGER, TYPE_TELEX, TYPE_TTY_TDD, TYPE_WORK_MOBILE, TYPE_WORK_PAGER
								itemEntry.Kind = ContactPhoneKind.Other;
								break;
						}
						entry.Phones.Add(itemEntry);

					}
					subCursor.Close();


					if (!searchFound && _queryOptions._whereToSearch == ContactQuerySearchFields.Phone)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}

				}

				// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.Email
				// ADDRESS, TYPE
				entry.Emails.Clear();

				if (desiredFields.HasFlag(ContactQueryDesiredFields.EmailAddress))
				{
					Android.Database.ICursor subCursor = _contentResolver.Query(
									Android.Provider.ContactsContract.Data.ContentUri,
									new string[] { "data1", "data2" }, //null,   // all columns
																	   // ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
									"contact_id = ? AND mimetype = ?",
									new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.Email.ContentItemType },
									null);   // default order

					//columnD1 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data1); // Email.ADDRESS
					//columnD2 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data2); // Email.TYPE
					for (int itemGuard = 10; itemGuard > 0 && subCursor.MoveToNext(); itemGuard--)
					{
						var itemEntry = new ContactEmail();
						itemEntry.Address = subCursor.GetString(0);     // we defined columns while opening cursor, so we know what data is in which columns
						if (!searchFound && _queryOptions._whereToSearch.HasFlag(ContactQuerySearchFields.Email))
						{
							if (itemEntry.Address.Contains(_queryOptions._whatToSearch))
								searchFound = true;
						}

						switch (subCursor.GetInt(1))    // we defined columns while opening cursor, so we know what data is in which columns
						{
							case 1: // TYPE_HOME
								itemEntry.Kind = ContactEmailKind.Personal;
								break;
							case 2:
								itemEntry.Kind = ContactEmailKind.Work;
								break;
							default:    // TYPE_MOBILE, TYPE_OTHER
								itemEntry.Kind = ContactEmailKind.Other;
								break;
						}
						entry.Emails.Add(itemEntry);
					}
					subCursor.Close();

					if (!searchFound && _queryOptions._whereToSearch == ContactQuerySearchFields.Email)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}
				}

				// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.StructuredName
				// DISPLAY_NAME, GIVEN_NAME, FAMILY_NAME, PREFIX, MIDDLE_NAME, SUFFIX, PHONETIC_GIVEN_NAME, PHONETIC_MIDDLE_NAME, PHONETIC_FAMILY_NAME
				{
					Android.Database.ICursor subCursor = _contentResolver.Query(
										Android.Provider.ContactsContract.Data.ContentUri,
										new string[] { "data1", "data2", "data3", "data4", "data5", "data6" }, //null,   // all columns
																											   // ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
										"contact_id = ? AND mimetype = ?",
										new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.StructuredName.ContentItemType },
										null);   // default order

					//columnD1 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data1); // DISPLAY_NAME
					//columnD2 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data2); // 	GIVEN_NAME
					//int columnD3 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data3); // 	FAMILY_NAME
					//int columnD4 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data4); // 	PREFIX
					//int columnD5 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data5); // 	MIDDLE_NAME
					//int columnD6 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data6); // 	SUFFIX
					//int columnD7 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data7); // 	PHONETIC_GIVEN_NAME
					//int columnD8 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data8); // 	PHONETIC_MIDDLE_NAME
					//int columnD9 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data9); // 	PHONETIC_FAMILY_NAME

					if (subCursor.MoveToNext())
					{

						// entry.FullName { get; internal set; }
						entry.MiddleName = subCursor.GetString(4);       // we defined columns while opening cursor, so we know what data is in which columns
						entry.LastName = subCursor.GetString(2);         // we defined columns while opening cursor, so we know what data is in which columns
						entry.FirstName = subCursor.GetString(1);        // we defined columns while opening cursor, so we know what data is in which columns
						entry.HonorificNamePrefix = subCursor.GetString(3);  // we defined columns while opening cursor, so we know what data is in which columns
						entry.HonorificNameSufix = subCursor.GetString(5);   // we defined columns while opening cursor, so we know what data is in which columns
						entry.DisplayName = subCursor.GetString(1);      // we defined columns while opening cursor, so we know what data is in which columns

						if (!searchFound && _queryOptions._whereToSearch.HasFlag(ContactQuerySearchFields.Name))
						{
							if (entry.MiddleName.Contains(_queryOptions._whatToSearch) ||
									entry.LastName.Contains(_queryOptions._whatToSearch) ||
									entry.FirstName.Contains(_queryOptions._whatToSearch) ||
									entry.HonorificNamePrefix.Contains(_queryOptions._whatToSearch) ||
									entry.HonorificNameSufix.Contains(_queryOptions._whatToSearch) ||
									entry.DisplayName.Contains(_queryOptions._whatToSearch))
								searchFound = true;
						}


					}
					subCursor.Close();

					if (!searchFound && _queryOptions._whereToSearch == ContactQuerySearchFields.Name)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}
				}

				//// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.StructuredPostal
				//// 	FORMATTED_ADDRESS, TYPE, LABEL, STREET, POBOX, NEIGHBORHOOD, CITY, REGION, POSTCODE, COUNTRY

				entry.Addresses.Clear();
				if (desiredFields.HasFlag(ContactQueryDesiredFields.PostalAddress))
				{
					Android.Database.ICursor subCursor = _contentResolver.Query(
									Android.Provider.ContactsContract.Data.ContentUri,
									new string[] { "data2", "data4", "data7", "data8", "data9", "data10" }, //null,// all columns
																											// ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
									"raw_contact_id = ? AND mimetype = ?",
									new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.StructuredPostal.ContentItemType },
									null);   // default order

					//// columnD1 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data1); // FORMATTED_ADDRESS
					//columnD2 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data2); // TYPE
					////columnD3 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data3); // LABEL
					//columnD4 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data4); // STREET
					////columnD5 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data5); // POBOX
					////columnD6 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data6); // NEIGHBORHOOD
					//int columnD7 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data7); // CITY
					//int columnD8 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data8); // REGION (state, province..)
					//int columnD9 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data9); // POSTCODE
					//int columnDA = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data10); // COUNTRY
					// columns usage based on: https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.contacts.contact

					for (int itemGuard = 10; itemGuard > 0 && subCursor.MoveToNext(); itemGuard--)
					{
						var itemEntry = new ContactAddress();
						itemEntry.StreetAddress = subCursor.GetString(1);    // we defined columns while opening cursor, so we know what data is in which columns
						itemEntry.Region = subCursor.GetString(3);
						itemEntry.PostalCode = subCursor.GetString(4);
						itemEntry.Locality = subCursor.GetString(2);
						//itemEntry.Description = subCursor.GetString(columnD4);
						itemEntry.Country = subCursor.GetString(5);

						if (!searchFound && _queryOptions._whereToSearch == ContactQuerySearchFields.All)
						{
							if (itemEntry.StreetAddress.Contains(_queryOptions._whatToSearch) ||
									itemEntry.Region.Contains(_queryOptions._whatToSearch) ||
									itemEntry.PostalCode.Contains(_queryOptions._whatToSearch) ||
									itemEntry.Locality.Contains(_queryOptions._whatToSearch) ||
									itemEntry.Country.Contains(_queryOptions._whatToSearch))
								searchFound = true;
						}



						switch (subCursor.GetInt(0))    // we defined columns while opening cursor, so we know what data is in which columns
						{
							case 1: // TYPE_HOME
								itemEntry.Kind = ContactAddressKind.Home;
								break;
							case 2:
								itemEntry.Kind = ContactAddressKind.Work;
								break;
							default:    // TYPE_OTHER
								itemEntry.Kind = ContactAddressKind.Other;
								break;
						}

						entry.Addresses.Add(itemEntry);
					}
					subCursor.Close();

					if (!searchFound && _queryOptions._whereToSearch != ContactQuerySearchFields.None)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}
				}

				entriesList.Add(entry);
			}

			return entriesList;
		}

	}

	public partial class ContactBatch
	{

		public IReadOnlyList<Contact> Contacts;

		internal ContactBatch(IReadOnlyList<Contact> contacts )
		{
			Contacts = contacts;
		}

	}

	public partial class Contact
	{
		// public string Name { get; set } // can be unimplemented, see firstname (and lastname?)
		// https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.contacts.contact.name

		public IList<ContactEmail> Emails { get; internal set; }
		// Address (321 char), Description (512 char), Kind
		// KIND: Personal, Work, Other

		public IList<ContactAddress> Addresses { get; internal set; }
		// Country (1024), Kind, PostalCode (zip, 1024), Region (state, 1024), StreetAddress (street, 1024), Description (512), Locality (city, 1024)
		// KIND: Home, Work, Other

		public IList<ContactPhone> Phones { get; internal set; }
		// Description (512), Kind, Number (50)
		// KIND: 10 roznych, Home, Mobile, Work, Other, Pager, BusinessFax, HomeFax, Company, Assistant, Radio

		// public string FullName { get; internal set; }

		public string MiddleName { get; set; } // 64

		public string LastName { get; set; } // 64

		public string FirstName { get; set; } // 64 char

		public string HonorificNamePrefix { get; set; } // 32

		public string HonorificNameSufix { get; set; } // 32

		public string DisplayName { get; internal set; }

		public Contact()
		{
			Emails = new List<ContactEmail>();
			Phones = new List<ContactPhone>();
			Addresses = new List<ContactAddress>();
		}

	}

	public enum ContactAddressKind
	{
		Home,
		Work,
		Other,
	}

	public partial class ContactAddress
	{
		public string StreetAddress { get; set; }
		public string Region { get; set; }
		public string PostalCode { get; set; }
		public string Locality { get; set; }
		public ContactAddressKind Kind { get; set; }
		// public string Description { get; set; }
		public string Country { get; set; }
		public ContactAddress()
		{
			// overriding 'missing method'
		}
	}

	public enum ContactPhoneKind
		{
			Home,
			Mobile,
			Work,
			Other,
			Pager,
			BusinessFax,
			HomeFax,
			Company,
			Assistant,
			Radio,
		}

	public partial class ContactPhone
	{
		public string Number { get; set; }
		public ContactPhoneKind Kind { get; set; }
		public ContactPhone()
		{
			// overriding 'missing method'
		}
	}


	public enum ContactEmailKind
	{
		Personal,
		Work,
		Other,
	}

	public partial class ContactEmail
	{
		public ContactEmailKind Kind { get; set;}

		public string Address { get; set; } // 321 chars

		// public string Description { get; set; } // 512 chars
		public ContactEmail()
		{
			// overriding error - method lost
		}
	}


#endregion

#region "calllog"

	public partial class PhoneCallHistoryManager
	{

		private static async Task<PhoneCallHistoryStore> RequestStoreAsyncTask(Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType accessType)
		{
			// UWP: AppEntriesReadWrite, AllEntriesLimitedReadWrite, AllEntriesReadWrite
			// Android: Manifest has READ_CALL_LOG and WRITE_CALL_LOG, no difference between app/limited/full
			// using: AllEntriesReadWrite as ReadWrite, and AllEntriesLimitedReadWrite as ReadOnly


			var _histStore = new PhoneCallHistoryStore();

			// below API 16 (JellyBean), permission are granted
			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.JellyBean)
			{
				return _histStore;
			}

			// since API 29, we should do something more:
			// https://developer.android.com/reference/android/content/pm/PackageInstaller.SessionParams.html#setWhitelistedRestrictedPermissions(java.util.Set%3Cjava.lang.String%3E)

			// do we have declared this permission in Manifest?
			// it could be also Coarse, without GPS
			Android.Content.Context context = Android.App.Application.Context;
			Android.Content.PM.PackageInfo packageInfo =
				context.PackageManager.GetPackageInfo(context.PackageName, Android.Content.PM.PackageInfoFlags.Permissions);
			var requestedPermissions = packageInfo?.RequestedPermissions;
			if (requestedPermissions is null)
				return null;

			bool bInManifest = false;
			foreach (string oPerm in requestedPermissions)
			{
				if (oPerm.Equals(Android.Manifest.Permission.ReadCallLog, StringComparison.OrdinalIgnoreCase))
					bInManifest = true;
			}

			if (!bInManifest)
				return null;

			// required for contact name
			bInManifest = false;
			foreach (string oPerm in requestedPermissions)
			{
				if (oPerm.Equals(Android.Manifest.Permission.ReadContacts, StringComparison.OrdinalIgnoreCase))
					bInManifest = true;
			}

			if (!bInManifest)
				return null;



			if (accessType == Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType.AllEntriesReadWrite)
			{
				bInManifest = false;
				foreach (string oPerm in requestedPermissions)
				{
					if (oPerm.Equals(Android.Manifest.Permission.WriteCallLog, StringComparison.OrdinalIgnoreCase))
						bInManifest = true;
				}

				if (!bInManifest)
					return null;
			}

			List<string> requestPermission = new List<string>();

			// check if permission is granted
			if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Uno.UI.ContextHelper.Current, Android.Manifest.Permission.ReadCallLog)
					!= Android.Content.PM.Permission.Granted)
			{
				requestPermission.Add(Android.Manifest.Permission.ReadCallLog);
			}

			if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Uno.UI.ContextHelper.Current, Android.Manifest.Permission.ReadContacts)
					!= Android.Content.PM.Permission.Granted)
			{
				requestPermission.Add(Android.Manifest.Permission.ReadContacts);
			}

			if (accessType == Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType.AllEntriesReadWrite)
			{
				if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Uno.UI.ContextHelper.Current, Android.Manifest.Permission.WriteCallLog)
						!= Android.Content.PM.Permission.Granted)
				{
					requestPermission.Add(Android.Manifest.Permission.WriteCallLog);
				}
			}

			if (requestPermission.Count < 1)
				return _histStore;

			// system dialog asking for permission

			// this code would not compile here - but it compile in your own app.
			// to be compiled inside Uno, it has to be splitted into layers
			var tcs = new TaskCompletionSource<Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs>();

			void handler(object sender, Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs e)
			{

				if (e.RequestCode == 1)
				{
					tcs.TrySetResult(e);
				}
			}

			var current = Uno.UI.BaseActivity.Current;

			try
			{
				current.RequestPermissionsResultWithResults += handler;

				Android.Support.V4.App.ActivityCompat.RequestPermissions(Uno.UI.BaseActivity.Current, requestPermission.ToArray(), 1);

				var result = await tcs.Task;
				if (result.GrantResults.Length < 1)
					return null;
				if (result.GrantResults[0] == Android.Content.PM.Permission.Granted)
					return _histStore;

			}
			finally
			{
				current.RequestPermissionsResultWithResults -= handler;
			}


			return null;

			//}

			return _histStore;
		}

		public static IAsyncOperation<PhoneCallHistoryStore> RequestStoreAsync(Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType accessType)
			=> RequestStoreAsyncTask(accessType).AsAsyncOperation<PhoneCallHistoryStore>();
	}

	public partial class PhoneCallHistoryStore
	{
		public PhoneCallHistoryEntryReader GetEntryReader() => new PhoneCallHistoryEntryReader();

	}

	// https://developer.samsung.com/galaxy/others/calllogs-in-android
	public partial class PhoneCallHistoryEntryReader
	{
		// <uses-permission android:name="android.permission.READ_CONTACTS">  ? A nie calllog?

		private Android.Database.ICursor _cursor = null;

		public PhoneCallHistoryEntryReader()
		{
			Android.Content.ContentResolver cr = Android.App.Application.Context.ContentResolver;

			_cursor = cr.Query(Android.Provider.CallLog.Calls.ContentUri,
									null,
									null,
									null,
									Android.Provider.CallLog.Calls.DefaultSortOrder);   // == date DESC

			string sTmp = MyDUmps.DumpTableHeaderNames(_cursor);
		}

		~PhoneCallHistoryEntryReader()
		{
			if (_cursor != null)
			{
				_cursor.Close();
			}
		}

		public async Task<IReadOnlyList<Windows.ApplicationModel.Calls.PhoneCallHistoryEntry>> ReadBatchAsync()
		{

			var entriesList = new List<Windows.ApplicationModel.Calls.PhoneCallHistoryEntry>();

			if (_cursor is null)
			{
				return entriesList;
			}

			for (int pageGuard = 100; pageGuard > 0 && _cursor.MoveToNext(); pageGuard--)
			{
				var entry = new Windows.ApplicationModel.Calls.PhoneCallHistoryEntry();

				int callType = _cursor.GetInt(_cursor.GetColumnIndex(Android.Provider.CallLog.Calls.Type));
				switch (callType)
				{
					case 1:
						entry.IsIncoming = true;
						break;
					case 3:
						entry.IsMissed = true;
						break;
					case 4:
						entry.IsVoicemail = true;
						break;
				}

				// https://developer.android.com/reference/android/provider/CallLog.Calls - seconds
				// https://developer.samsung.com/galaxy/others/calllogs-in-android - miliseconds
				entry.Duration = TimeSpan.FromSeconds(_cursor.GetLong(_cursor.GetColumnIndex(Android.Provider.CallLog.Calls.Duration)));

				entry.StartTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0,
						TimeSpan.FromMilliseconds(_cursor.GetLong(_cursor.GetColumnIndex(Android.Provider.CallLog.Calls.Date))));

				entry.Address = new Windows.ApplicationModel.Calls.PhoneCallHistoryEntryAddress(
					_cursor.GetString(_cursor.GetColumnIndex(Android.Provider.CallLog.Calls.Number)),
					Windows.ApplicationModel.Calls.PhoneCallHistoryEntryRawAddressKind.PhoneNumber);

				try
				{
					entry.Address.DisplayName = _cursor.GetString(_cursor.GetColumnIndex(Android.Provider.CallLog.Calls.CachedName));
				}
				catch
				{
					// can be null
				}

				entriesList.Add(entry);
			}

			return entriesList;
		}
	}


#endregion

#region "SMS"

	public partial interface IChatItem
	{
		Windows.ApplicationModel.Chat.ChatItemKind ItemKind { get; }
	}

	public enum ChatMessageKind	// ale ze to niby moze byc juz zdefiniowane
	{
		Standard,
		FileTransferRequest,
		TransportCustom,
		JoinedConversation,
		LeftConversation,
		OtherParticipantJoinedConversation,
		OtherParticipantLeftConversation,
	}

	public enum ChatMessageOperatorKind // ale ze to niby moze byc juz zdefiniowane
	{
		Unspecified,
		Sms,
		Mms,
		Rcs
	}

	public partial class ChatMessage : IChatItem
	{
		public Windows.ApplicationModel.Chat.ChatItemKind ItemKind { get; }

		public bool IsIncoming { get; set; }
		// public bool IsForwardingDisabled
		//public string TransportId
		public Windows.ApplicationModel.Chat.ChatMessageStatus Status { get; set; }
		public string From { get; set; }
		// public string Subject
		public bool IsRead { get; set; }
		public DateTimeOffset NetworkTimestamp { get; set; }
		public DateTimeOffset LocalTimestamp { get; set; }
		// public global::System.Collections.Generic.IReadOnlyDictionary<string, global::Windows.ApplicationModel.Chat.ChatMessageStatus> RecipientSendStatuses
		// public string TransportFriendlyName
		// public global::System.Collections.Generic.IList<global::Windows.ApplicationModel.Chat.ChatMessageAttachment> Attachments
		// public string Id
		public bool IsSeen { get; set; }
		public ChatMessageKind MessageKind { get; set; }
		// public bool IsReceivedDuringQuietHours
		//public bool IsAutoReply
		// public ulong EstimatedDownloadSize
		// public global::Windows.ApplicationModel.Chat.ChatConversationThreadingInfo ThreadingInfo
		// public bool ShouldSuppressNotification
		// public string RemoteId
		public ChatMessageOperatorKind MessageOperatorKind { get; set; }
		// public bool IsReplyDisabled
		// public bool IsSimMessage
		// public global::System.Collections.Generic.IList<global::Windows.ApplicationModel.Chat.ChatRecipientDeliveryInfo> RecipientsDeliveryInfos
		// public string SyncId

	}


	/*
	Windows.ApplicationModel.Chat.ChatMessageManager.RequestStoreAsync()
	oStore.GetMessageReader()

	oRdr.ReadBatchAsync()

	oMsg.IsIncoming
	oMsg.From
	oMsg.LocalTimestamp

	dodac ewentualnie zamiane vbCrLf na "\n" przed zapisaniem


	no i nie ma tez pliku:
	Windows.Storage.KnownFolders.RemovableDevices
	externalDevices.GetFoldersAsync() [lista kart SD]

	do importu:
	Windows.Storage.Pickers.FileOpenPicker()
	picker.FileTypeFilter
	picker.PickSingleFileAsync()

	Windows.Storage.FileIO.ReadTextAsync

	obu stron:
	oMsg.MessageKind = Windows.ApplicationModel.Chat.ChatMessageKind.Standard;
	oMsg.MessageOperatorKind = Windows.ApplicationModel.Chat.ChatMessageOperatorKind.Sms;

	oMsg.IsIncoming
	oMsg.From
	oMsg.IsRead
	oMsg.IsSeen
	oMsg.Status
	oMsg.LocalTimestamp
	oMsg.NetworkTimestamp

	oStore.SaveMessageAsync */

#endregion

}


#endif 