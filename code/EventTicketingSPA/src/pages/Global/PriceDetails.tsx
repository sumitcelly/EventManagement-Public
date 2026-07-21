import "../../components/PricingTable";
import { PricingTable } from "../../components/PricingTable";
import AppNavbar from "../../components/Navbar";
import { IonContent, IonHeader, IonPage } from "@ionic/react";
import Footer  from '../../components/Footer';

export default function PricingDetails() {
return(
     <IonPage>
        <IonHeader>
            <AppNavbar />
        </IonHeader>
        <IonContent className="ion-padding flex flex-col justify-center items-center h-full">
            <PricingTable eventId={0} />
          
        </IonContent>
        <Footer/>
    </IonPage>

)}