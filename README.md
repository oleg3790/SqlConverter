## Overview

Service that will convert (vA specific) SQL DML (update, insert and merge) into select and partially validate parameters

## Usage

Instantiate a converter service object, check validity of SQL; and invoke the convert call.

```c#
var converter = new SqlConverterService(SQL, LogicalId);

if (converter.IsSqlValid())
{
    string convertedSQL = converter.Convert();
}               
else
{
    string status = converter.GetValidationErrorMessage();
}     
```

## Logging

Log4Net is used as the logging service, just configure Log4Net in the consuming application.
