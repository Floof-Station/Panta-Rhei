# Idk man, i'm not an erp person. Come up with something better if you can.
# "-others-" popups exist mostly to discourage people from doing it in public too much. They are only visible in a range of 5 tiles and not visible to ghosts.
#
# There are no error popups because those can be kinda immersion-breaking and not very useful when there's already a popup at the start of the action.


# ==================================================================================================================== #
# LewdFillContainerBreast
interaction-LewdFillContainerBreast-name = Lactate (into container)
# User == Target
interaction-LewdFillContainerBreast-delayed-self-popup = You are trying to milk yourself...
interaction-LewdFillContainerBreast-success-self-popup = You squeeze out a bit of milk into {THE($used)}.
# Others.
interaction-LewdFillContainerBreast-delayed-others-popup = Seems to be messing around with {THE($used)}...
interaction-LewdFillContainerBreast-success-others-popup = {THE($user)} fills {THE($used)} with some fluid from their body.


interaction-LewdFillContainerPenis-name = Cum (into container)
# User == Target
interaction-LewdFillContainerPenis-delayed-self-popup = You are trying to cum into {THE($used)}...
interaction-LewdFillContainerPenis-success-self-popup = You finish inside {THE($used)}, filling it with cum.
# Others.
interaction-LewdFillContainerPenis-delayed-others-popup = {interaction-LewdFillContainerBreast-delayed-others-popup}
interaction-LewdFillContainerPenis-success-others-popup = {interaction-LewdFillContainerBreast-success-others-popup}

# Frankly idfk what you call this, I'm only adding this because there are recipes that need natural lubricant.
interaction-LewdFillContainerVagina-name = Squirt (into container)
# User == Target
interaction-LewdFillContainerVagina-delayed-self-popup = You are trying to squirt...
interaction-LewdFillContainerVagina-success-self-popup = You squirt some liquid into {THE($used)}.
# Others.
interaction-LewdFillContainerVagina-delayed-others-popup = {interaction-LewdFillContainerBreast-delayed-others-popup}
interaction-LewdFillContainerVagina-success-others-popup = {interaction-LewdFillContainerBreast-success-others-popup}



# ==================================================================================================================== #
interaction-LewdFillContainerBreastOther-name = Milk (into container)
# User == Target
interaction-LewdFillContainerBreastOther-delayed-self-popup = You are trying to milk {THE($target)}...
interaction-LewdFillContainerBreastOther-success-self-popup = You squeeze out a bit of milk from {THE($target)}'s breast into {THE($used)}.
# Others.
interaction-LewdFillContainerBreastOther-delayed-others-popup = Seems to be trying to milk {THE($target)}...
interaction-LewdFillContainerBreastOther-success-others-popup = {THE($user)} fills {THE($used)} with some fluid from {THE($target)}'s body.


interaction-LewdFillContainerPenisOther-name = Make cum (into container)
# User == Target
interaction-LewdFillContainerPenisOther-delayed-self-popup = You are trying to help {THE($target)} cum into {THE($used)}...
interaction-LewdFillContainerPenisOther-success-self-popup = You make {THE($target)} finish inside {THE($used)}, filling it with cum.
# Others.
interaction-LewdFillContainerPenisOther-delayed-others-popup = {interaction-LewdFillContainerBreastOther-delayed-others-popup}
interaction-LewdFillContainerPenisOther-success-others-popup = {interaction-LewdFillContainerBreastOther-success-others-popup}



# ==================================================================================================================== #
interaction-LewdSuckBreastOther-name = Suck breast
# Target
interaction-LewdSuckBreastOther-delayed-target-popup = You feel {THE($user)} suck on your nipples.
interaction-LewdSuckBreastOther-success-target-popup = {THE($user)} sucks some milk out of your breast.
# User
interaction-LewdSuckBreastOther-delayed-self-popup = You are trying to suck {THE($target)}'s breast.
interaction-LewdSuckBreastOther-success-self-popup = You feel some milk flow out of {THE($target)}'s breast and into your mouth.
# Others. I couldn't come up with anything better.
interaction-LewdSuckBreastOther-delayed-others-popup = {THE($user)} seems to be sucking {THE($target)}'s breast...
interaction-LewdSuckBreastOther-success-others-popup = {interaction-LewdSuckPenisOther-delayed-others-popup}


interaction-LewdSuckPenisOther-name = Suck penis
# Target
interaction-LewdSuckPenisOther-delayed-target-popup = {THE($user)} is trying to suck your penis...
interaction-LewdSuckPenisOther-success-target-popup = {THE($user)} makes you cum and fill {POSS-ADJ($user)} mouth!
# User
interaction-LewdSuckPenisOther-delayed-self-popup = You are trying to suck {THE($target)}'s penis.
interaction-LewdSuckPenisOther-success-self-popup = You feel {THE($target)}'s cum fill your mouth as they finish inside.
# Others
interaction-LewdSuckPenisOther-delayed-others-popup = {THE($user)} seems to be sucking {THE($target)} off...
interaction-LewdSuckPenisOther-success-others-popup = {THE($target)} seems to cum inside {THE($user)} mouth.



# ==================================================================================================================== #
interaction-LewdFillPV-name = Cum in pussy
# Target
interaction-LewdFillPV-delayed-target-popup = {THE($user)} starts pounding your pussy...
interaction-LewdFillPV-success-target-popup = {THE($user)} cums inside you, coating your pussy white!
# User
interaction-LewdFillPV-delayed-self-popup = You start pounding {THE($target)}'s pussy...
interaction-LewdFillPV-success-self-popup = You cum inside {THE($target)}, coating {POSS-ADJ($target)} ass white!
# Others
interaction-LewdFillPV-delayed-others-popup = {THE($user)} seems to be pounding {THE($target)}...
interaction-LewdFillPV-success-others-popup = {THE($user)} seems to finish inside {THE($target)}.


interaction-LewdFillPR-name = Cum in ass
# Target
interaction-LewdFillPR-delayed-target-popup = {THE($user)} starts pounding your ass...
interaction-LewdFillPR-success-target-popup = {THE($user)} cums inside you, coating your ass white!
# User
interaction-LewdFillPR-delayed-self-popup = You start pounding {THE($target)}'s ass...
interaction-LewdFillPR-success-self-popup = You cum inside {THE($target)}, coating {POSS-ADJ($target)} ass white!
# Others - we're reusing shit
interaction-LewdFillPR-delayed-others-popup = {interaction-LewdFillPV-delayed-target-popup}
interaction-LewdFillPR-success-others-popup = {interaction-LewdFillPV-success-others-popup}



# ==================================================================================================================== #
# LewdSpillVagina
interaction-LewdSpillVagina-name = Climax (on the floor)
# User == Target
interaction-LewdSpillVagina-delayed-self-popup = You are trying to reach climax...
interaction-LewdSpillVagina-success-self-popup = You climax, spilling the contents of your pussy onto the floor!
# Others
interaction-LewdSpillVagina-delayed-others-popup = {THE($user)} looks like {SUBJECT($user)} {CONJUGATE-BE($target)} about to climax...
interaction-LewdSpillVagina-success-others-popup = {THE($user)} climaxes, splashing something onto the floor!

interaction-LewdSpillPenis-name = Cum (on the floor)
# User == Target
interaction-LewdSpillPenis-delayed-self-popup = You are trying to cum...
interaction-LewdSpillPenis-success-self-popup = You cum, spilling a white substance onto the floor!
# Others
interaction-LewdSpillPenis-delayed-others-popup = {THE($user)} looks like {SUBJECT($user)} {CONJUGATE-BE($target)} about to cum...
interaction-LewdSpillPenis-success-others-popup = {THE($user)} cums, splashing something onto the floor!
