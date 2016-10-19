using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;

namespace WebRole1.LiveFrame
{
    public partial class CivilAddress : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            /*
            BasicGeoposition queryHint = new BasicGeoposition();
            //queryHint.Latitude = props.Latitude.Value;
            //queryHint.Longitude = props.Longitude.Value;
            Geopoint hintPoint = new Geopoint(queryHint);

            MapLocationFinderResult result =
                await MapLocationFinder.FindLocationsAtAsync(hintPoint);

            Response.Write("Civil Address");
            Response.End();
            */
        }
    }
}