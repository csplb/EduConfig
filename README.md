EduConfig
=========

[![Build status](https://ci.appveyor.com/api/projects/status/3nbi23psd3i7ofd5?svg=true)](https://ci.appveyor.com/project/ktos/educonfig)

This project is an automatic configurator for the [eduroam network](http://eduroam.pollub.pl) at [Lublin University of Technology](http://pollub.pl). It automatically installs our certificates and network profile with all parameters set up properly.

At this moment EduConfig allows to configure any Windows Vista or newer operating system for eduroam with EAP-PEAP authentication or EAP-TLS. However, as of version 1.0.3, EAP-TLS is only available from the command line parameters.

## Usage

Run EduConfig.exe (with elevated privileges - it will automatically restart in elevated mode if run without), agree with message, wait a little. Done!

## Command line usage

It is possible to run EduConfig from command line with parameter `/s` which starts in silent mode, without any confirmations or messages, reporting status only with exit code. If you use `/s` and `/tls`, EduConfig will install an alternative configuration for EAP-TLS. A user certificate, however, needs to be installed separately by hand.

--ktos
