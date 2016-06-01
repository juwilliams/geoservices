using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gbc.Constants
{
    public class ExceptionConstants
    {
        public class Messages
        {
            public class Configuration
            {
                public const string FILE_IN_ARCHIVE_NOT_SET = "The required shapefile file name in the gzipped tar archive was not specified in the harvester configuration. No data will be transmitted to service.";
                public const string OUTPUT_FILE_NOT_SET = "The required output file for the gzipped tar archive was not specified in the harvester configuration. No data will be transmitted to service.";
            }

            public class SDE
            {
                public const string COULD_NOT_GET_SEARCH_CURSOR_FOR_SHAPEFILE = "An exception occured while attempting to retrieve an ICursor for the specified input shapefile.";
            }

        }

        public static class ResponseExceptions
        {
            public const string NO_RESPONSE = "No response from remote server";
            public const string NO_DATA_RETURNED = "Remote server response contained no records";
            public const string ERROR_CONTACTING_REMOTE_SERVER = "An error was encountered while attempting to contact the remote server";
        }
    }
}
