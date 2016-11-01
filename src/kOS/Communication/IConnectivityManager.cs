namespace kOS.Communication
{
    public interface IConnectivityManager
    {
        /// <summary>
        /// Intended for internal use, so that the ConnectivityManager class can determine if
        /// the module implementing this interface is currently able to run.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Classes implementing this interface should return true for this property if autopilot
        /// event subscription can be cleared without notifying our classes and as such they
        /// should expect to periodically "resubscribe" to the events.
        /// </summary>
        bool NeedAutopilotResubscribe { get; }

        /// <summary>
        /// Get the signal delay between two given vessels.
        /// </summary>
        /// <param name="vessel1"></param>
        /// <param name="vessel2"></param>
        /// <returns>The delay in seconds.  If there is no connection, this should return -1</returns>
        double GetDelay(Vessel vessel1, Vessel vessel2);

        /// <summary>
        /// Get the signal delay between the given vessel and "Home".  "Home" is a concept inherited from
        /// stock CommNet and represents KSC or other ground stations.  It does not represent a connection
        /// to a command station.  This concept of "home" should be adapted, but remain essentially the
        /// same concept, for other mods that may support additional ground stations.
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns>The delay in seconds.  If there is no connection, this should return -1</returns>
        double GetDelayToHome(Vessel vessel);

        /// <summary>
        /// Get the signal delay between the given vessel and the control source.  This control source
        /// may be the "home" discussed in the GetDelayToHome summary, or any other command station
        /// allowing control of the given vessel.  The control source may even be the local vessel, if
        /// it pilot control is valid for the implementation.
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns>The delay in seconds.  If there is no connection, this should return -1</returns>
        double GetDelayToControl(Vessel vessel);

        /// <summary>
        /// Determine if there is a connection between the given vessel and "home".  See the commentary
        /// on "home" in the summary of GetDelayToHome.
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns>True if there is a connection, False if no connection is available</returns>
        bool HasConnectionToHome(Vessel vessel);

        /// <summary>
        /// Determine if there is a connection to a control source.  See the comentary in the summary
        /// of GetDelayToControl.  This method is used to determine if terminal input is currently
        /// allowed.
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns>True if there is a connection, False if no connection is available</returns>
        bool HasConnectionToControl(Vessel vessel);

        /// <summary>
        /// Determine if there is a connection betweeen the two given vessels.
        /// </summary>
        /// <param name="vessel1"></param>
        /// <param name="vessel2"></param>
        /// <returns>The delay in seconds.  If there is no connection, this should return -1</returns>
        bool HasConnection(Vessel vessel1, Vessel vessel2);

        /// <summary>
        /// This method subscribes to autopilot events to allow kOS to control things like steering and
        /// throttle.  This allows another mod to block the functionality of the stock events, while
        /// still leting mods like kOS serve their autopilot purpose.
        /// </summary>
        /// <param name="vessel"></param>
        /// <param name="hook"></param>
        void AddAutopilotHook(Vessel vessel, FlightInputCallback hook);

        /// <summary>
        /// This method unsubscribes to autopilot events, effectively releasing kOS's control over a vessel
        /// as well as preventing issues with stale references.
        /// </summary>
        /// <param name="vessel"></param>
        /// <param name="hook"></param>
        void RemoveAutopilotHook(Vessel vessel, FlightInputCallback hook);
    }
}