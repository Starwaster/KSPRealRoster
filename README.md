KSPRealRoster
=============

This is a mod for KSP, intended to fix a perceived oversight in the manner in which crew assignment is managed. It performs three functions, any of which are optional.

	(*) Enable or Disable automatic crewing of vessels in the editor. If you disable this crewing, you will need to manually select any crew to be added to a vessel, or when you reach the launchpad or runway your vessel will not have crew. This option is enabled by default.
	
	(*) Allow for randomization of the crew assigned automatically to a vessel in the editor. This option is enabled by default.
	
	(*) Maintain a blacklist of crew members who should never be automatically selected for crewing a vessel. These crew members can be selected manually, though. This blacklist is empty by default.
	
These features may be configured either by editing RealRoster.cfg located at '<KSP Installation Directory>/GameData/Enneract/Plugins/RealRoster.cfg', with the following values:
	
	crewAssignment = (True/False) (Controls automatic crewing)
	crewRandomization = (True/False) (Controls crew randomization)
	blackList
	{
		Item = (Kerbal Name)
		Item = ...
	}
	
Alternatively, if you use Blizzy's Toolbar mod, an additional toolbar icon will be available to allow you to configure these settings in-game. The Toolbar mod is not, however, required to use this plugin.

