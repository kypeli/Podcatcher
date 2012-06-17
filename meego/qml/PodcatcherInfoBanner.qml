import QtQuick 1.1
import com.meego 1.0
import com.nokia.extras 1.0

Component {
    Connections {
        target: ui
        onShowInfoBanner: {
            console.log("Showing banner: "+text);
            uiInfoBanner.text = text
            uiInfoBanner.show();
        }
    }

    InfoBanner {
        id: uiInfoBanner
        topMargin: 10
        leftMargin: 10
    }
}
